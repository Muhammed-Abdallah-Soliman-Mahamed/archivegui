namespace Archive.Models
{
    /// <summary>
    /// اللي المستخدم اختاره للضغط (ملفات و/أو مجلدات) في صورتين:
    /// Roots = المسارات الأصلية (للأدوات الخارجية اللي بتضغط مجلد كامل)،
    /// Files = قائمة الملفات المسطّحة بأسمائها داخل الأرشيف (لـ SharpCompress).
    /// بنحافظ على بنية المجلدات: مجلد "Docs" بيتضغط كـ "Docs/sub/file.txt" مش "file.txt".
    /// </summary>
    public sealed class ArchiveInput
    {
        public IReadOnlyList<string> Roots { get; }
        public IReadOnlyList<ArchiveSource> Files { get; }

        private ArchiveInput(List<string> roots, List<ArchiveSource> files)
        {
            Roots = roots;
            Files = files;
        }

        public long TotalBytes => Files.Sum(f => new FileInfo(f.FullPath).Length);

        public static ArchiveInput From(IEnumerable<string> paths)
        {
            var roots = new List<string>();
            var files = new List<ArchiveSource>();

            foreach (var raw in paths)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(raw));
                var parent = Path.GetDirectoryName(full) ?? full;

                if (File.Exists(full))
                {
                    roots.Add(full);
                    files.Add(new ArchiveSource(full, Path.GetFileName(full)));
                }
                else if (Directory.Exists(full))
                {
                    roots.Add(full);
                    foreach (var file in Directory.EnumerateFiles(full, "*", SearchOption.AllDirectories))
                    {
                        var entryName = Path.GetRelativePath(parent, file)
                            .Replace(Path.DirectorySeparatorChar, '/');
                        files.Add(new ArchiveSource(file, entryName));
                    }
                }
            }

            if (roots.Count == 0)
                throw new InvalidOperationException("مفيش ملفات أو مجلدات موجودة للضغط.");

            return new ArchiveInput(roots, files);
        }
    }
}
