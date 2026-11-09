using Archive.Models;

namespace Archive.Interface
{
    public interface IArchiveService
    {
        Task<List<ArchiveEntry>> ListEntriesAsync(string archivePath, string? password = null);

        Task ExtractAsync(
            string archivePath,
            string destinationFolder,
            IProgress<double>? progress = null,
            string? password = null);

        Task CreateAsync(
            IEnumerable<string> sourcePaths,
            string destinationArchivePath,
            IProgress<double>? progress = null,
            ArchiveCreationOptions? options = null);
    }
}
