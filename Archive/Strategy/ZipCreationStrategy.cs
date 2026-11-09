using Archive.Interface;
using Archive.Models;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Writers.Zip;

namespace Archive.Strategy
{
    /// <summary>
    /// Zip العادي (من غير باسورد) عن طريق SharpCompress.
    /// لو فيه باسورد، الـ Factory بيحوّل الطلب لـ ExternalToolCreationStrategy.
    /// </summary>
    public class ZipCreationStrategy : IArchiveCreationStrategy
    {
        public bool CanHandle(string destinationArchivePath, ArchiveCreationOptions options) =>
            !options.IsEncrypted
            && Path.GetExtension(destinationArchivePath).Equals(".zip", StringComparison.OrdinalIgnoreCase);

        public async Task CreateAsync(
            ArchiveInput input,
            string destinationArchivePath,
            ArchiveCreationOptions options,
            Action<long> reportBytesWritten)
        {
            await using var archive = await ZipArchive.CreateAsyncArchive();

            foreach (var source in input.Files)
            {
                var fileInfo = new FileInfo(source.FullPath);
                await archive.AddEntryAsync(source.EntryName, fileInfo);
                reportBytesWritten(fileInfo.Length);
            }

            // File.Create بدل OpenWrite: لو الملف موجود قبل كده وأطول، مفيش بقايا قديمة في آخره.
            await using var outputStream = File.Create(destinationArchivePath);
            var writerOptions = new ZipWriterOptions(CompressionType.Deflate);
            await archive.SaveToAsync(outputStream, writerOptions);
        }
    }
}
