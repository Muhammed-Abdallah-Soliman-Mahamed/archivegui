using Archive.Models;

namespace ArchiveGui.Models;

/// <summary>
/// خيار تشفير في الدروب ليست (بدون تشفير / AES-256 / ZipCrypto).
/// </summary>
public record EncryptionOption(string DisplayName, EncryptionMethod Method)
{
    public override string ToString() => DisplayName;
}
