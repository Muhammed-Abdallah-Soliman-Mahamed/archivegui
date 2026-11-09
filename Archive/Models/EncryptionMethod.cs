namespace Archive.Models
{
    /// <summary>
    /// نوع التشفير المستخدم عند إنشاء الأرشيف.
    /// </summary>
    public enum EncryptionMethod
    {
        /// <summary>من غير باسورد.</summary>
        None = 0,

        /// <summary>التشفير التقليدي لـ Zip — ضعيف ومكسور تقنيًا، موجود للتوافق مع البرامج القديمة فقط.</summary>
        ZipCrypto = 1,

        /// <summary>AES-256 — الخيار الموصى به (Zip / 7z / RAR).</summary>
        Aes256 = 2,
    }
}
