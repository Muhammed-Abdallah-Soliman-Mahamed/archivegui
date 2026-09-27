using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Archive.Interface
{
    public interface IArchiveService
    {
        Task<List<ArchiveEntry>> ListEntriesAsync(string archivePath);

        Task ExtractAsync(string archivePath, string destinationFolder, IProgress<double>? progress = null);
Task CreateAsync(IEnumerable<string> sourcePaths, string destinationArchivePath, IProgress<double>? progress = null);
    }
}