using SharpCompress.Archives;
using Archive.Interface;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Writers.Zip;

namespace Archive.Services
{
    public class SharpCompressArchiveService : IArchiveService
    {
        public async Task<List<ArchiveEntry>> ListEntriesAsync(string archivePath)
        {
            var entries = new List<ArchiveEntry>();
            await using (var archive = await ArchiveFactory.OpenAsyncArchive(archivePath))
            {
                await foreach (var entry in archive.EntriesAsync)
                {
                    entries.Add(new ArchiveEntry(
                        entry.Key,
                        entry.Size,
                        entry.CompressedSize,
                        entry.IsDirectory,
                        entry.LastModifiedTime ?? DateTime.Now
                    ));
                }
            }
            return entries;
        }
        public async Task CreateAsync(IEnumerable<string> sourcePaths, string destinationArchivePath, IProgress<double> progress = null)
{
    var filesToCompress = new List<string>();

    foreach (var path in sourcePaths)
    {
        if (File.Exists(path))
            filesToCompress.Add(path);
        else if (Directory.Exists(path))
            filesToCompress.AddRange(Directory.GetFiles(path, "*", SearchOption.AllDirectories));
    }

    long totalBytes = filesToCompress.Sum(file => new FileInfo(file).Length);
    long processedBytes = 0;

    void ReportProgress(long fileLength)
    {
        processedBytes += fileLength;
        if (totalBytes > 0)
            progress?.Report((double)processedBytes / totalBytes * 100);
    }

    var strategy = ArchiveCreationStrategyFactory.Resolve(destinationArchivePath);
    await strategy.CreateAsync(filesToCompress, destinationArchivePath, ReportProgress);
}
        public async Task ExtractAsync(string archivePath, string destinationFolder, IProgress<double> progress = null)
        {
            Directory.CreateDirectory(destinationFolder);
            await using (var archive = await ArchiveFactory.OpenAsyncArchive(archivePath))
            {
                long totalSize = 0;
                await foreach (var entry in archive.EntriesAsync)
                {
                    if (!entry.IsDirectory)
                    {
                        totalSize += entry.Size;
                    }
                }

                long extractedSize = 0;
                await foreach (var entry in archive.EntriesAsync)
                {
                    if (!entry.IsDirectory)
                    {
                        await entry.WriteToDirectoryAsync(destinationFolder, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                    }

                    extractedSize += entry.Size;
                    if (totalSize > 0)
                    {
                        progress?.Report((double)extractedSize / totalSize * 100);
                    }
                }
            }
        }
    }
}