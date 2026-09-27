using Archive.Interface;
using Archive.Strategy;
using SharpCompress.Common;

namespace Archive.Services
{
    /// <summary>
    /// نقطة الدخول الوحيدة من الخارج: بتاخد مسار الملف وترجع الاستراتيجية المناسبة.
    /// أي صيغة جديدة تتضاف هنا بس، من غير ما تلمس SharpCompressArchiveService خالص.
    /// </summary>
    public static class ArchiveCreationStrategyFactory
    {
        private static readonly List<IArchiveCreationStrategy> Strategies = new()
        {
            new ZipCreationStrategy(),
            new SevenZipCreationStrategy(),
            new RarCreationStrategy(),
            new TarCreationStrategy(CompressionType.None, path => path.EndsWith(".tar")),
            new TarCreationStrategy(CompressionType.GZip, path => path.EndsWith(".tar.gz") || path.EndsWith(".tgz")),
            new TarCreationStrategy(CompressionType.BZip2, path => path.EndsWith(".tar.bz2")),
        };

        public static IArchiveCreationStrategy Resolve(string destinationArchivePath)
        {
            var strategy = Strategies.FirstOrDefault(s => s.CanHandle(destinationArchivePath));

            if (strategy is null)
            {
                var extension = Path.GetExtension(destinationArchivePath);
                throw new NotSupportedException($"صيغة الأرشيف '{extension}' غير مدعومة للإنشاء.");
            }

            return strategy;
        }
    }
}