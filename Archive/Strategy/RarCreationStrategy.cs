using Archive.Interface;
using SharpCompress.Common;

namespace Archive.Strategy
{
    /// <summary>
    /// نقطة الدخول الوحيدة من الخارج: بتاخد مسار الملف وترجع الاستراتيجية المناسبة.
    /// أي صيغة جديدة تتضاف هنا بس، من غير ما تلمس SharpCompressArchiveService خالص.
    /// </summary>
    public class RarCreationStrategy : IArchiveCreationStrategy
    {
        public bool CanHandle(string destinationArchivePath) =>
            Path.GetExtension(destinationArchivePath).Equals(".rar", StringComparison.OrdinalIgnoreCase);

        public Task CreateAsync(List<string> filesToCompress, string destinationArchivePath, Action<long> reportBytesWritten)
        {
            throw new NotSupportedException(
                "إنشاء ملفات RAR غير مدعوم — خوارزمية ضغط RAR ملكية خاصة ومغلقة المصدر. " +
                "لو محتاج RAR فعلاً، استخدم أداة WinRAR نفسها عبر Process.Start.");
        }
    }
}