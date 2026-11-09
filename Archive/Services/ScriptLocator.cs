namespace Archive.Services
{
    /// <summary>
    /// بيلاقي سكربتات bash اللي بتنزل مع التطبيق (مجلد scripts).
    /// الترتيب: متغير بيئة ARCHIVEGUI_SCRIPTS، جنب الـ executable، بعدين مسارات التثبيت على لينكس.
    /// </summary>
    public static class ScriptLocator
    {
        public static string Find(string scriptName)
        {
            var searched = new List<string>();

            foreach (var dir in CandidateDirectories())
            {
                var path = Path.Combine(dir, scriptName);
                searched.Add(path);
                if (File.Exists(path)) return path;
            }

            throw new FileNotFoundException(
                $"سكربت '{scriptName}' مش موجود. اتدوّر عليه في: {string.Join(", ", searched)}");
        }

        private static IEnumerable<string> CandidateDirectories()
        {
            var fromEnv = Environment.GetEnvironmentVariable("ARCHIVEGUI_SCRIPTS");
            if (!string.IsNullOrWhiteSpace(fromEnv)) yield return fromEnv;

            yield return Path.Combine(AppContext.BaseDirectory, "scripts");
            yield return "/usr/lib/archivegui/scripts";
            yield return "/usr/share/archivegui/scripts";
            yield return "/opt/archivegui/scripts";
        }
    }
}
