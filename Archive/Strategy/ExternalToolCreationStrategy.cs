using Archive.Interface;
using Archive.Models;
using Archive.Services;

namespace Archive.Strategy
{
    /// <summary>
    /// بتنشئ الأرشيف عن طريق سكربت bash (archivegui-create.sh) اللي بيستدعي 7z / rar / tar / zip.
    /// الضغط نفسه بيحصل في السكربت، والـ C# بس بيشغّله ويمرّر له الباسورد كمتغير بيئة.
    /// </summary>
    public sealed class ExternalToolCreationStrategy : IArchiveCreationStrategy
    {
        private readonly string _format;
        private readonly Func<string, bool> _matchesExtension;
        private readonly bool _requiresScripts;

        /// <param name="format">الصيغة اللي السكربت بيفهمها: zip / 7z / rar / tar / tar.gz / tar.bz2</param>
        /// <param name="matchesExtension">بتاخد المسار بحروف صغيرة.</param>
        /// <param name="requiresScripts">
        /// true للصيغ اللي SharpCompress بيعرف يعملها (zip/tar): الاستراتيجية دي بتتفعّل بس لو
        /// السكربتات متاحة، وإلا بنرجع لـ SharpCompress. 7z و rar مفيش لهم بديل فبيتفعّلوا دايمًا.
        /// </param>
        public ExternalToolCreationStrategy(string format, Func<string, bool> matchesExtension, bool requiresScripts)
        {
            _format = format;
            _matchesExtension = matchesExtension;
            _requiresScripts = requiresScripts;
        }

        public bool CanHandle(string destinationArchivePath, ArchiveCreationOptions options) =>
            _matchesExtension(destinationArchivePath.ToLowerInvariant())
            && (!_requiresScripts || ExternalTools.ScriptsAvailable);

        public async Task CreateAsync(
            ArchiveInput input,
            string destinationArchivePath,
            ArchiveCreationOptions options,
            Action<long> reportBytesWritten)
        {
            if (!ExternalTools.HasBash)
                throw new NotSupportedException(
                    "الصيغة دي محتاجة bash، وهو مش متاح على النظام ده.");

            var script = ScriptLocator.Find("archivegui-create.sh");

            var args = new List<string> { _format, Path.GetFullPath(destinationArchivePath) };
            args.AddRange(input.Roots);

            var env = new Dictionary<string, string>();
            if (options.IsEncrypted)
            {
                env["ARCHIVEGUI_PASSWORD"] = options.Password!;
                env["ARCHIVEGUI_ENCRYPTION"] =
                    options.Encryption == EncryptionMethod.ZipCrypto ? "zipcrypto" : "aes256";
            }

            var result = await BashScriptRunner.RunAsync(script, args, env);
            if (!result.Success)
                throw new InvalidOperationException(result.ErrorMessage);

            reportBytesWritten(input.TotalBytes);
        }
    }
}
