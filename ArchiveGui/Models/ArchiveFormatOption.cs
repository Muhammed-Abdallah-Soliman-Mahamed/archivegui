namespace ArchiveGui.Models;

/// <summary>
/// خيار صيغة في الدروب ليست. SupportsPassword = الصيغة تقدر تتشفّر بباسورد (Zip / 7z / RAR).
/// </summary>
public record ArchiveFormatOption(string DisplayName, string Extension, bool SupportsPassword = false)
{
    public override string ToString() => DisplayName;
}
