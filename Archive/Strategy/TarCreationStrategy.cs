using Archive.Interface;
using Archive.Models;
using SharpCompress.Archives;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;
using SharpCompress.Writers.Tar;

namespace Archive.Strategy
{
    /// <summary>
    /// بتغطي .tar, .tar.gz/.tgz, .tar.bz2 حسب الـ CompressionType الممرر في الـ constructor.
    /// صيغة tar نفسها مفيهاش تشفير، فلو اتطلب باسورد بنرفض بدل ما نطلّع أرشيف مش محمي.
    /// </summary>
    public class TarCreationStrategy : IArchiveCreationStrategy
    {
        private readonly CompressionType _compressionType;
        private readonly Func<string, bool> _matchesExtension;

        public TarCreationStrategy(CompressionType compressionType, Func<string, bool> matchesExtension)
        {
            _compressionType = compressionType;
            _matchesExtension = matchesExtension;
        }

        public bool CanHandle(string destinationArchivePath, ArchiveCreationOptions options) =>
            _matchesExtension(destinationArchivePath.ToLowerInvariant());

        public async Task CreateAsync(
            ArchiveInput input,
            string destinationArchivePath,
            ArchiveCreationOptions options,
            Action<long> reportBytesWritten)
        {
            if (options.IsEncrypted)
                throw new NotSupportedException(
                    "صيغ tar مفيهاش تشفير بالباسورد. اختار Zip أو 7z أو RAR لو عايز باسورد.");

            await using var archive = await TarArchive.CreateAsyncArchive();

            foreach (var source in input.Files)
            {
                var fileInfo = new FileInfo(source.FullPath);
                await archive.AddEntryAsync(source.EntryName, fileInfo);
                reportBytesWritten(fileInfo.Length);
            }

            await using var outputStream = File.Create(destinationArchivePath);
            // true = finalize archive on close (لازم، وإلا ملف tar بيطلع تالف: "Failed to read TAR header")
            var writerOptions = new TarWriterOptions(_compressionType, true);
            await archive.SaveToAsync(outputStream, writerOptions);
        }
    }
}
