using Archive.Interface;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Writers.Zip;

namespace Archive.Strategy
{
    public class ZipCreationStrategy : IArchiveCreationStrategy
    {
        public bool CanHandle(string destinationArchivePath)
        {
            return Path.GetExtension(destinationArchivePath).Equals(".zip", StringComparison.OrdinalIgnoreCase);
        }

        public async Task CreateAsync(List<string> filesToCompress, string destinationArchivePath, Action<long> reportBytesWritten)
        {
            await using var archive = await ZipArchive.CreateAsyncArchive();

            foreach (var filePath in filesToCompress)
            {
                var fileInfo = new FileInfo(filePath);
                await archive.AddEntryAsync(fileInfo.Name, fileInfo);
                reportBytesWritten(fileInfo.Length);
            }

            await using var outputStream = File.OpenWrite(destinationArchivePath);
            var options = new ZipWriterOptions(CompressionType.Deflate);
            await archive.SaveToAsync(outputStream, options);
        }

    }
}