using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Archive.Services
{
    public sealed record BashResult(int ExitCode, string StdOut, string StdErr)
    {
        public bool Success => ExitCode == 0;

        /// <summary>أنسب رسالة خطأ للعرض: stderr لو موجود، وإلا آخر سطور stdout.</summary>
        public string ErrorMessage
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(StdErr)) return StdErr.Trim();

                var lines = StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return lines.Length > 0
                    ? string.Join(Environment.NewLine, lines.TakeLast(6))
                    : $"السكربت انتهى بكود خروج {ExitCode}.";
            }
        }
    }

    /// <summary>
    /// بيشغّل سكربت bash من غير ما يعدّي من shell: الوسائط بتتمرر كـ ArgumentList
    /// (يعني مفيش مشاكل مسافات أو علامات اقتباس في أسماء الملفات)، والباسورد بيتمرر
    /// كمتغير بيئة مش كوسيط في سطر الأوامر.
    /// </summary>
    public static class BashScriptRunner
    {
        public static async Task<BashResult> RunAsync(
            string scriptPath,
            IEnumerable<string>? args = null,
            IDictionary<string, string>? environment = null,
            string? workingDirectory = null,
            Action<string>? onOutputLine = null,
            bool elevate = false,
            CancellationToken cancellationToken = default)
        {
            var psi = new ProcessStartInfo
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            if (elevate)
            {
                psi.FileName = "pkexec";
                psi.ArgumentList.Add("bash");
            }
            else
            {
                psi.FileName = "bash";
            }

            psi.ArgumentList.Add(scriptPath);
            if (args != null)
                foreach (var a in args) psi.ArgumentList.Add(a);

            if (environment != null)
                foreach (var kv in environment) psi.Environment[kv.Key] = kv.Value;

            if (!string.IsNullOrEmpty(workingDirectory))
                psi.WorkingDirectory = workingDirectory;

            using var process = new Process { StartInfo = psi };

            try
            {
                process.Start();
            }
            catch (Win32Exception ex)
            {
                throw new InvalidOperationException(
                    $"تعذّر تشغيل '{psi.FileName}' — هل هو مثبّت على الجهاز؟ ({ex.Message})", ex);
            }

            // السكربتات بتدير الـ stdin بنفسها؛ نقفله عشان أي أداة تسأل سؤال متعلّقش.
            process.StandardInput.Close();

            using var cancelRegistration = cancellationToken.Register(() =>
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
                catch { /* العملية خلصت بالفعل */ }
            });

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            var outTask = PumpAsync(process.StandardOutput, stdout, onOutputLine);
            var errTask = PumpAsync(process.StandardError, stderr, onOutputLine);

            await process.WaitForExitAsync(CancellationToken.None);
            await Task.WhenAll(outTask, errTask);

            cancellationToken.ThrowIfCancellationRequested();

            return new BashResult(process.ExitCode, stdout.ToString(), stderr.ToString());
        }

        private static async Task PumpAsync(StreamReader reader, StringBuilder sink, Action<string>? onLine)
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                sink.AppendLine(line);
                onLine?.Invoke(line);
            }
        }
    }
}
