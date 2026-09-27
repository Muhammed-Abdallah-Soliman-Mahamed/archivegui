using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Archive.Interface;
using SharpCompress.Archives.SevenZip;

namespace Archive.Strategy
{
    public class SevenZipCreationStrategy : IArchiveCreationStrategy
    {
        public bool CanHandle(string destinationArchivePath) =>
            Path.GetExtension(destinationArchivePath).Equals(".7z", StringComparison.OrdinalIgnoreCase);

        public Task CreateAsync(List<string> filesToCompress, string destinationArchivePath, Action<long> reportBytesWritten)
        {
            throw new NotSupportedException(
                "إنشاء ملفات 7z غير مدعوم في SharpCompress — الصيغة معقدة وغير تدفقية (non-streamable)، " +
                "والمكتبة توفر قراءة (extract) فقط. استخدم .zip بدلاً منها، " +
                "أو استدعِ أداة 7-Zip command-line (7z.exe) خارجيًا عبر Process.Start لو محتاج .7z فعلاً.");
        }
    }
}