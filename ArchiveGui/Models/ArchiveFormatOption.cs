namespace ArchiveGui.Models;

/// <summary>
/// يمثل صيغة أرشيف واحدة تظهر في الـ Dropdown. الاسم المعروض (DisplayName) للمستخدم،
/// والامتداد (Extension) هو اللي بيتضاف فعليًا لاسم الملف.
///
/// ملحوظة: 7z و RAR متعمدين مش موجودين هنا خالص، لأن CreateAsync في الخدمة
/// بترميلهم NotSupportedException — بمنعهم من القايمة من الأساس، المستخدم
/// مش هيقدر يحاول ينشئهم أصلاً، فمش محتاجين نتعامل مع الخطأ في الواجهة.
/// </summary>
public record ArchiveFormatOption(string DisplayName, string Extension)
{
    public override string ToString() => DisplayName;
}
