using Archive.Models;

namespace Archive.Interface
{
    public interface IArchiveCreationStrategy
    {
        /// <summary>هل الاستراتيجية دي تقدر تنشئ الأرشيف ده (حسب الامتداد وخيارات التشفير)؟</summary>
        bool CanHandle(string destinationArchivePath, ArchiveCreationOptions options);

        Task CreateAsync(
            ArchiveInput input,
            string destinationArchivePath,
            ArchiveCreationOptions options,
            Action<long> reportBytesWritten);
    }
}
