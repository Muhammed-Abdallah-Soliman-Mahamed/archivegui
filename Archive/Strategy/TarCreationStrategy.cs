using Archive.Interface;
using SharpCompress.Archives;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;
using SharpCompress.Writers.Tar;

namespace Archive.Strategy
{
    /// <summary>
    /// بتغطي .tar, .tar.gz/.tgz, .tar.bz2 حسب الـ CompressionType الممرر في الـ constructor.
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

        public bool CanHandle(string destinationArchivePath) => _matchesExtension(destinationArchivePath.ToLowerInvariant());

        public async Task CreateAsync(List<string> filesToCompress, string destinationArchivePath, Action<long> reportBytesWritten)
        {
            await using var archive = await TarArchive.CreateAsyncArchive();

            foreach (var filePath in filesToCompress)
            {
                var fileInfo = new FileInfo(filePath);
                await archive.AddEntryAsync(fileInfo.Name, fileInfo);
                reportBytesWritten(fileInfo.Length);
            }

            await using var outputStream = File.OpenWrite(destinationArchivePath);
            var options = new TarWriterOptions(_compressionType, true);
            await archive.SaveToAsync(outputStream, options);
        }
    }
}