using Archive.Interface;
using Archive.Models;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace Archive.Services
{
    public class SharpCompressArchiveService : IArchiveService
    {
        private const string PasswordHint = "الأرشيف محمي بباسورد — اكتب الباسورد وحاول تاني.";

        public async Task<List<ArchiveEntry>> ListEntriesAsync(string archivePath, string? password = null)
        {
            if (!string.IsNullOrEmpty(password))
                return await ListWithScriptAsync(archivePath, password);

            var entries = new List<ArchiveEntry>();
            try
            {
                await using var archive = await ArchiveFactory.OpenAsyncArchive(archivePath);
                await foreach (var entry in archive.EntriesAsync)
                {
                    entries.Add(new ArchiveEntry(
                        entry.Key ?? "",
                        entry.Size,
                        entry.CompressedSize,
                        entry.IsDirectory,
                        entry.LastModifiedTime ?? DateTime.Now));
                }
            }
            catch (Exception ex) when (LooksLikePasswordProblem(ex))
            {
                throw new InvalidOperationException(PasswordHint, ex);
            }

            return entries;
        }

        public async Task CreateAsync(
            IEnumerable<string> sourcePaths,
            string destinationArchivePath,
            IProgress<double>? progress = null,
            ArchiveCreationOptions? options = null)
        {
            options ??= ArchiveCreationOptions.None;

            if (options.Encryption != EncryptionMethod.None && string.IsNullOrEmpty(options.Password))
                throw new ArgumentException("اخترت التشفير بس الباسورد فاضي — اكتب باسورد أو اختار 'بدون تشفير'.");

            var input = ArchiveInput.From(sourcePaths);
            var destination = Path.GetFullPath(destinationArchivePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            long totalBytes = input.TotalBytes;
            long processedBytes = 0;

            void ReportProgress(long fileLength)
            {
                processedBytes += fileLength;
                if (totalBytes > 0)
                    progress?.Report(Math.Min(100d, (double)processedBytes / totalBytes * 100));
            }

            var strategy = ArchiveCreationStrategyFactory.Resolve(destination, options);
            await strategy.CreateAsync(input, destination, options, ReportProgress);

            progress?.Report(100);
        }

        public async Task ExtractAsync(
            string archivePath,
            string destinationFolder,
            IProgress<double>? progress = null,
            string? password = null)
        {
            // SharpCompress مبينشئش مجلد الوجهة تلقائي
            Directory.CreateDirectory(destinationFolder);

            if (ShouldUseScript(archivePath, password))
            {
                await ExtractWithScriptAsync(archivePath, destinationFolder, password);
                progress?.Report(100);
                return;
            }

            if (!string.IsNullOrEmpty(password))
                throw new NotSupportedException("فك الأرشيفات المحمية بباسورد محتاج bash و 7-Zip (شغّل install-tools.sh).");

            try
            {
                await using var archive = await ArchiveFactory.OpenAsyncArchive(archivePath);

                long totalSize = 0;
                await foreach (var entry in archive.EntriesAsync)
                    if (!entry.IsDirectory) totalSize += entry.Size;

                long extractedSize = 0;
                await foreach (var entry in archive.EntriesAsync)
                {
                    if (!entry.IsDirectory)
                    {
                        await entry.WriteToDirectoryAsync(
                            destinationFolder,
                            new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
                    }

                    extractedSize += entry.Size;
                    if (totalSize > 0)
                        progress?.Report((double)extractedSize / totalSize * 100);
                }
            }
            catch (Exception ex) when (LooksLikePasswordProblem(ex))
            {
                throw new InvalidOperationException(PasswordHint, ex);
            }
        }

        // ---------- bash / الأدوات الخارجية ----------

        /// <summary>
        /// الاستخراج بيتم بسكربت bash (archivegui-extract.sh) لكل الصيغ لما يكون متاح.
        /// SharpCompress احتياطي لما bash أو السكربتات مش موجودين (ويندوز).
        /// </summary>
        private static bool ShouldUseScript(string archivePath, string? password) =>
            ExternalTools.ScriptsAvailable;

        private static async Task ExtractWithScriptAsync(string archivePath, string destinationFolder, string? password)
        {
            var script = ScriptLocator.Find("archivegui-extract.sh");
            var result = await BashScriptRunner.RunAsync(
                script,
                new[] { Path.GetFullPath(archivePath), Path.GetFullPath(destinationFolder) },
                PasswordEnvironment(password));

            if (!result.Success)
                throw new InvalidOperationException(ExplainScriptFailure(result, password));
        }

        private static async Task<List<ArchiveEntry>> ListWithScriptAsync(string archivePath, string password)
        {
            if (!ExternalTools.HasBash)
                throw new NotSupportedException("عرض أرشيف بباسورد محتاج bash و 7-Zip.");

            var script = ScriptLocator.Find("archivegui-list.sh");
            var result = await BashScriptRunner.RunAsync(
                script,
                new[] { Path.GetFullPath(archivePath) },
                PasswordEnvironment(password));

            if (!result.Success)
                throw new InvalidOperationException(ExplainScriptFailure(result, password));

            var entries = new List<ArchiveEntry>();
            foreach (var line in result.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.TrimEnd('\r').Split('\t');
                if (parts.Length < 4) continue;

                long.TryParse(parts[1], out var size);
                long.TryParse(parts[2], out var packed);
                entries.Add(new ArchiveEntry(parts[0], size, packed, parts[3] == "1", DateTime.MinValue));
            }

            return entries;
        }

        private static Dictionary<string, string>? PasswordEnvironment(string? password) =>
            string.IsNullOrEmpty(password)
                ? null
                : new Dictionary<string, string> { ["ARCHIVEGUI_PASSWORD"] = password };

        private static string ExplainScriptFailure(BashResult result, string? password)
        {
            var message = result.ErrorMessage;
            if (message.Contains("wrong password", StringComparison.OrdinalIgnoreCase)
                || message.Contains("encrypted archive", StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrEmpty(password)
                    ? PasswordHint
                    : "الباسورد غلط، أو الأرشيف تالف.";
            }

            // 255 = 7z اتقفل stdin وهو بيسأل عن باسورد
            if (result.ExitCode == 255 && string.IsNullOrEmpty(password))
                return PasswordHint;

            return message;
        }

        private static bool LooksLikePasswordProblem(Exception ex) =>
            ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("encrypt", StringComparison.OrdinalIgnoreCase);
    }
}
