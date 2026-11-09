namespace Archive.Models
{
    /// <summary>
    /// ملف واحد هيتضغط: مساره الكامل على الجهاز + اسمه جوه الأرشيف (بمساره النسبي لو جاي من مجلد).
    /// </summary>
    public sealed record ArchiveSource(string FullPath, string EntryName);
}
