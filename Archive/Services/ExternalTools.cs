namespace Archive.Services
{
    /// <summary>
    /// بيفحص الأدوات الخارجية اللي ميزات الباسورد/7z/RAR بتعتمد عليها،
    /// عشان الواجهة تعرض الخيارات المتاحة فعلاً على الجهاز بس.
    /// </summary>
    public static class ExternalTools
    {
        public static bool HasBash => !OperatingSystem.IsWindows() && IsOnPath("bash");

        /// <summary>
        /// الإنشاء والاستخراج بيتنفّذوا بسكربتات bash (archivegui-create.sh / archivegui-extract.sh).
        /// لو bash أو السكربتات مش موجودين (ويندوز مثلاً) الخدمة بترجع لـ SharpCompress.
        /// </summary>
        public static bool ScriptsAvailable
        {
            get
            {
                if (!HasBash) return false;
                try
                {
                    ScriptLocator.Find("archivegui-create.sh");
                    ScriptLocator.Find("archivegui-extract.sh");
                    return true;
                }
                catch (FileNotFoundException)
                {
                    return false;
                }
            }
        }

        public static bool HasSevenZip => IsOnPath("7zz") || IsOnPath("7z") || IsOnPath("7za");

        public static bool HasRar => IsOnPath("rar");

        public static bool HasUnrar => IsOnPath("unrar");

        /// <summary>تقدر تفك RAR بالأدوات الخارجية (7z أو unrar أو rar).</summary>
        public static bool CanExtractRar => HasSevenZip || HasUnrar || HasRar;

        public static bool IsOnPath(string name)
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(path)) return false;

            foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    if (File.Exists(Path.Combine(dir, name))) return true;
                    if (OperatingSystem.IsWindows() && File.Exists(Path.Combine(dir, name + ".exe"))) return true;
                }
                catch
                {
                    // مسار غير صالح في PATH — نكمّل
                }
            }

            return false;
        }
    }
}
