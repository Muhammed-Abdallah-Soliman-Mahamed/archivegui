namespace Archive.Models
{
    /// <summary>
    /// خيارات إنشاء الأرشيف (حاليًا: الباسورد ونوع التشفير).
    /// </summary>
    public sealed record ArchiveCreationOptions(
        string? Password = null,
        EncryptionMethod Encryption = EncryptionMethod.None)
    {
        public static ArchiveCreationOptions None { get; } = new();

        /// <summary>true لو فيه تشفير مطلوب فعلاً (نوع تشفير + باسورد مش فاضي).</summary>
        public bool IsEncrypted =>
            Encryption != EncryptionMethod.None && !string.IsNullOrEmpty(Password);
    }
}
