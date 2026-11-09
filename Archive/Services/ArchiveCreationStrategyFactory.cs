using Archive.Interface;
using Archive.Models;
using Archive.Strategy;
using SharpCompress.Common;

namespace Archive.Services
{
    /// <summary>
    /// نقطة الدخول الوحيدة من الخارج: بتاخد مسار الملف وخيارات التشفير وترجع الاستراتيجية المناسبة.
    /// أي صيغة جديدة تتضاف هنا بس، من غير ما تلمس SharpCompressArchiveService خالص.
    /// الترتيب مهم: استراتيجيات السكربتات الأول، وSharpCompress احتياطي لو السكربتات مش متاحة.
    /// </summary>
    public static class ArchiveCreationStrategyFactory
    {
        private static readonly List<IArchiveCreationStrategy> Strategies = new()
        {
            // 1) سكربتات bash — المسار الأساسي لكل الصيغ
            new ExternalToolCreationStrategy("zip", p => p.EndsWith(".zip"), requiresScripts: true),
            new ExternalToolCreationStrategy("7z", p => p.EndsWith(".7z"), requiresScripts: false),
            new ExternalToolCreationStrategy("rar", p => p.EndsWith(".rar"), requiresScripts: false),
            new ExternalToolCreationStrategy("tar.gz", p => p.EndsWith(".tar.gz") || p.EndsWith(".tgz"), requiresScripts: true),
            new ExternalToolCreationStrategy("tar.bz2", p => p.EndsWith(".tar.bz2"), requiresScripts: true),
            new ExternalToolCreationStrategy("tar", p => p.EndsWith(".tar"), requiresScripts: true),

            // 2) احتياطي لما bash مش متاح (ويندوز مثلاً): SharpCompress
            new ZipCreationStrategy(),
            new TarCreationStrategy(CompressionType.None, path => path.EndsWith(".tar")),
            new TarCreationStrategy(CompressionType.GZip, path => path.EndsWith(".tar.gz") || path.EndsWith(".tgz")),
            new TarCreationStrategy(CompressionType.BZip2, path => path.EndsWith(".tar.bz2")),
        };

        public static IArchiveCreationStrategy Resolve(string destinationArchivePath, ArchiveCreationOptions options)
        {
            var strategy = Strategies.FirstOrDefault(s => s.CanHandle(destinationArchivePath, options));

            if (strategy is null)
            {
                var extension = Path.GetExtension(destinationArchivePath);
                throw new NotSupportedException($"صيغة الأرشيف '{extension}' غير مدعومة للإنشاء.");
            }

            return strategy;
        }
    }
}
