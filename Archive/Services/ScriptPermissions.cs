#pragma warning disable CA1416 // كل الاستدعاءات محمية بـ IsSupported
using System.Text.RegularExpressions;

namespace Archive.Services
{
    /// <summary>
    /// نسخة C# من chmod لملفات السكربتات (لينكس / ماك). مش بنستدعي أمر chmod خارجي،
    /// بنستخدم File.SetUnixFileMode مباشرة، فمفيش shell ولا مشاكل أسماء ملفات.
    /// </summary>
    public static class ScriptPermissions
    {
        private const UnixFileMode AnyExecute =
            UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

        public static bool IsSupported => !OperatingSystem.IsWindows();

        public static UnixFileMode GetMode(string path) => File.GetUnixFileMode(path);

        public static bool IsExecutable(string path) =>
            IsSupported && (GetMode(path) & AnyExecute) != 0;

        /// <summary>الصيغة الثمانية زي 755 أو 644.</summary>
        public static string ToOctal(string path) =>
            Convert.ToString((int)GetMode(path) & 0xFFF, 8).PadLeft(3, '0');

        /// <summary>الصيغة الرمزية زي rwxr-xr-x.</summary>
        public static string ToSymbolic(string path)
        {
            var m = GetMode(path);
            char Bit(UnixFileMode flag, char c) => (m & flag) != 0 ? c : '-';

            return string.Concat(
                Bit(UnixFileMode.UserRead, 'r'), Bit(UnixFileMode.UserWrite, 'w'), Bit(UnixFileMode.UserExecute, 'x'),
                Bit(UnixFileMode.GroupRead, 'r'), Bit(UnixFileMode.GroupWrite, 'w'), Bit(UnixFileMode.GroupExecute, 'x'),
                Bit(UnixFileMode.OtherRead, 'r'), Bit(UnixFileMode.OtherWrite, 'w'), Bit(UnixFileMode.OtherExecute, 'x'));
        }

        /// <summary>
        /// "+x" بيضيف صلاحية التنفيذ للكل، "-x" بيشيلها، وأي رقم ثماني (755 / 644 / 700 ...) بيتطبّق زي ما هو.
        /// </summary>
        public static void Apply(string path, string spec)
        {
            if (!IsSupported)
                throw new PlatformNotSupportedException("تغيير الصلاحيات (chmod) مش مدعوم على ويندوز.");

            switch (spec)
            {
                case "+x":
                    File.SetUnixFileMode(path, GetMode(path) | AnyExecute);
                    break;
                case "-x":
                    File.SetUnixFileMode(path, GetMode(path) & ~AnyExecute);
                    break;
                default:
                    if (!Regex.IsMatch(spec, "^[0-7]{3,4}$"))
                        throw new ArgumentException($"صيغة صلاحيات غير صالحة: '{spec}'");
                    File.SetUnixFileMode(path, (UnixFileMode)Convert.ToInt32(spec, 8));
                    break;
            }
        }
    }
}
