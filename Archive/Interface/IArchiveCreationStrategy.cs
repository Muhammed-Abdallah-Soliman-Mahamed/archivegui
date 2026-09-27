using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Archive.Interface
{
    public interface IArchiveCreationStrategy
    {
        bool CanHandle(string destinationArchivePath);

        Task CreateAsync(List<string> filesToCompress, string destinationArchivePath, Action<long> reportBytesWritten);

    }
}