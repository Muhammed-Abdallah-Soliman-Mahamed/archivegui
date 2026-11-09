using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Archive.Interface;
using Archive.Models;
using Archive.Services;
using ArchiveGui.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArchiveGui.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IArchiveService _service = new SharpCompressArchiveService();
    private readonly List<string> _selectedPaths = new();
    private CancellationTokenSource? _scriptCts;

    // ───────────── الصيغ والتشفير ─────────────

    public ObservableCollection<ArchiveFormatOption> AvailableFormats { get; } = new();

    [ObservableProperty] private IReadOnlyList<EncryptionOption> _availableEncryptions = Array.Empty<EncryptionOption>();
    [ObservableProperty] private ArchiveFormatOption? _selectedFormat;
    [ObservableProperty] private EncryptionOption? _selectedEncryption;

    // ───────────── إنشاء ─────────────

    [ObservableProperty] private string _selectedFilesText = "لم يتم اختيار أي شيء بعد";
    [ObservableProperty] private string _destinationFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    [ObservableProperty] private string _archiveFileName = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _confirmPassword = "";

    // ───────────── استخراج ─────────────

    [ObservableProperty] private string _sourceArchivePath = "";
    [ObservableProperty] private string _extractionFolder = "";
    [ObservableProperty] private string _extractPassword = "";
    public ObservableCollection<string> ArchiveEntries { get; } = new();

    // ───────────── الحالة العامة ─────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateArchiveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ListEntriesCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExtractArchiveCommand))]
    private bool _isBusy;

    [ObservableProperty] private double _progressValue;
    [ObservableProperty] private string _statusMessage = "جاهز";
    [ObservableProperty] private string _toolsStatusText = "";

    // ───────────── سكربتات Bash ─────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasScript))]
    [NotifyCanExecuteChangedFor(nameof(RunScriptCommand))]
    [NotifyCanExecuteChangedFor(nameof(ChmodAndRunCommand))]
    private string _scriptPath = "";

    [ObservableProperty] private string _scriptArguments = "";
    [ObservableProperty] private string _scriptModeText = "—";
    [ObservableProperty] private string _scriptOutput = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunScriptCommand))]
    [NotifyCanExecuteChangedFor(nameof(ChmodAndRunCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelScriptCommand))]
    private bool _isScriptRunning;

    public bool HasScript => !string.IsNullOrEmpty(ScriptPath);
    public bool IsUnixSupported => ScriptPermissions.IsSupported;

    public bool UsePassword => SelectedEncryption is { Method: not EncryptionMethod.None };

    public string DestinationPath
    {
        get
        {
            if (SelectedFormat is null
                || string.IsNullOrWhiteSpace(DestinationFolder)
                || string.IsNullOrWhiteSpace(ArchiveFileName))
                return "";

            var name = CleanFileName(ArchiveFileName);
            return name.Length == 0 ? "" : Path.Combine(DestinationFolder, name + SelectedFormat.Extension);
        }
    }

    public MainViewModel()
    {
        RefreshTools();
    }

    // ═════════════ الأدوات الخارجية (bash / 7z / rar) ═════════════

    private void RefreshTools()
    {
        var previous = SelectedFormat?.Extension;
        var previousEncryption = SelectedEncryption?.Method;

        var hasBash = ExternalTools.HasBash;

        // 7z و RAR بيظهروا دايمًا. لو الأداة ناقصة بنكتب جنبها "(الأداة ناقصة)" والعملية بتوقف
        // برسالة واضحة. الأدوات المفروض تكون متثبّتة مع الحزمة قبل فتح البرنامج (مش بتتحمّل منه).
        string Suffix(bool installed) => installed ? "" : "  (الأداة ناقصة)";

        AvailableFormats.Clear();
        AvailableFormats.Add(new ArchiveFormatOption("Zip  (.zip)", ".zip", hasBash));
        AvailableFormats.Add(new ArchiveFormatOption("Tar  (.tar)", ".tar"));
        AvailableFormats.Add(new ArchiveFormatOption("Tar + GZip  (.tar.gz)", ".tar.gz"));
        AvailableFormats.Add(new ArchiveFormatOption("Tar + BZip2  (.tar.bz2)", ".tar.bz2"));
        if (hasBash)
        {
            AvailableFormats.Add(new ArchiveFormatOption(
                "7-Zip  (.7z)" + Suffix(ExternalTools.HasSevenZip), ".7z", true));
            AvailableFormats.Add(new ArchiveFormatOption(
                "RAR  (.rar)" + Suffix(ExternalTools.HasRar), ".rar", true));
        }

        SelectedFormat = AvailableFormats.FirstOrDefault(f => f.Extension == previous) ?? AvailableFormats[0];

        // مسح القايمة ممكن يصفّر اختيار التشفير، فنرجّعه زي ما كان
        if (previousEncryption is { } method)
            SelectedEncryption = AvailableEncryptions.FirstOrDefault(o => o.Method == method) ?? SelectedEncryption;

        if (!hasBash)
        {
            ToolsStatusText = "bash غير متاح على النظام — الباسورد و 7z و RAR معطّلين.";
        }
        else
        {
            static string Mark(bool ok) => ok ? "مثبّت" : "غير مثبّت";
            ToolsStatusText =
                $"السكربتات: {Mark(ExternalTools.ScriptsAvailable)}   ·   7-Zip: {Mark(ExternalTools.HasSevenZip)}   ·   rar: {Mark(ExternalTools.HasRar)}   ·   unrar: {Mark(ExternalTools.HasUnrar)}";
            if (!ExternalTools.HasSevenZip || !ExternalTools.HasRar)
                ToolsStatusText += "\nالناقص بيتثبّت مرة واحدة من الطرفية:  sudo bash /usr/lib/archivegui/scripts/install-tools.sh";
        }
    }

    // ═════════════ اختيار المصادر ═════════════

    public void AddSelectedPaths(IEnumerable<string> paths)
    {
        foreach (var p in paths)
            if (!string.IsNullOrWhiteSpace(p) && !_selectedPaths.Contains(p))
                _selectedPaths.Add(p);

        if (string.IsNullOrWhiteSpace(ArchiveFileName) && _selectedPaths.Count > 0)
            ArchiveFileName = CleanFileName(Path.GetFileName(_selectedPaths[0]));

        UpdateSelectionText();
    }

    [RelayCommand]
    private void ClearSelection()
    {
        _selectedPaths.Clear();
        UpdateSelectionText();
    }

    private void UpdateSelectionText()
    {
        if (_selectedPaths.Count == 0)
        {
            SelectedFilesText = "لم يتم اختيار أي شيء بعد";
            return;
        }

        var names = _selectedPaths.Take(3)
            .Select(p => Path.GetFileName(p) + (Directory.Exists(p) ? "/" : ""));
        SelectedFilesText = $"{_selectedPaths.Count} عنصر:  {string.Join(" ، ", names)}"
                            + (_selectedPaths.Count > 3 ? " …" : "");
    }

    public void SetDestinationFolder(string path) => DestinationFolder = path;
    public void SetExtractionFolder(string path) => ExtractionFolder = path;

    public void SetSourceArchive(string path)
    {
        SourceArchivePath = path;
        ArchiveEntries.Clear();
        if (string.IsNullOrWhiteSpace(ExtractionFolder))
            ExtractionFolder = Path.Combine(Path.GetDirectoryName(path) ?? ".", StripArchiveExtension(path));
    }

    // ═════════════ إنشاء ═════════════

    [RelayCommand(CanExecute = nameof(CanRunArchiveOp))]
    private async Task CreateArchiveAsync()
    {
        if (_selectedPaths.Count == 0) { StatusMessage = "اختار ملفات أو مجلد الأول."; return; }
        if (string.IsNullOrEmpty(DestinationPath)) { StatusMessage = "حدّد مجلد الحفظ واسم الملف."; return; }
        if (File.Exists(DestinationPath)) { StatusMessage = "الملف ده موجود بالفعل — غيّر الاسم أو احذفه الأول."; return; }

        ArchiveCreationOptions options = ArchiveCreationOptions.None;
        if (UsePassword)
        {
            if (string.IsNullOrEmpty(Password)) { StatusMessage = "اكتب باسورد، أو اختار 'بدون تشفير'."; return; }
            if (Password != ConfirmPassword) { StatusMessage = "الباسورد وتأكيده مش متطابقين."; return; }
            options = new ArchiveCreationOptions(Password, SelectedEncryption!.Method);
        }

        IsBusy = true;
        ProgressValue = 0;
        StatusMessage = "جاري الإنشاء…";
        try
        {
            var destination = DestinationPath;
            if (File.Exists(destination)) { StatusMessage = "الملف ده موجود بالفعل — غيّر الاسم أو احذفه الأول."; return; }

            StatusMessage = "جاري الإنشاء…";
            var progress = new Progress<double>(p => ProgressValue = p);
            await _service.CreateAsync(_selectedPaths, destination, progress, options);

            StatusMessage = $"تم الإنشاء: {destination}";
            Password = "";
            ConfirmPassword = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ═════════════ عرض واستخراج ═════════════

    [RelayCommand(CanExecute = nameof(CanRunArchiveOp))]
    private async Task ListEntriesAsync()
    {
        if (!File.Exists(SourceArchivePath)) { StatusMessage = "اختار ملف الأرشيف الأول."; return; }

        IsBusy = true;
        try
        {
            ArchiveEntries.Clear();
            var entries = await _service.ListEntriesAsync(SourceArchivePath, NullIfEmpty(ExtractPassword));

            foreach (var e in entries)
            {
                ArchiveEntries.Add(e.IsDirectory
                    ? e.Path.TrimEnd('/') + "/"
                    : $"{e.Path}    ({FormatSize(e.Size)})");
            }

            StatusMessage = $"{entries.Count} عنصر في الأرشيف.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunArchiveOp))]
    private async Task ExtractArchiveAsync()
    {
        if (!File.Exists(SourceArchivePath)) { StatusMessage = "اختار ملف الأرشيف الأول."; return; }
        if (string.IsNullOrWhiteSpace(ExtractionFolder)) { StatusMessage = "حدّد مجلد الاستخراج."; return; }

        IsBusy = true;
        ProgressValue = 0;
        StatusMessage = "جاري الاستخراج…";
        try
        {
            StatusMessage = "جاري الاستخراج…";
            var progress = new Progress<double>(p => ProgressValue = p);
            await _service.ExtractAsync(SourceArchivePath, ExtractionFolder, progress, NullIfEmpty(ExtractPassword));

            ProgressValue = 100;
            StatusMessage = $"تم الاستخراج إلى: {ExtractionFolder}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanRunArchiveOp() => !IsBusy;

    // ═════════════ سكربتات Bash ═════════════

    public void SetScript(string path)
    {
        ScriptPath = path;
        RefreshScriptMode();
        ScriptOutput = "";
        AppendScriptOutput("── معاينة (أول 60 سطر) ──");
        try
        {
            foreach (var line in File.ReadLines(path).Take(60))
                AppendScriptOutput(line);
        }
        catch (Exception ex)
        {
            AppendScriptOutput($"(تعذّرت القراءة: {ex.Message})");
        }
        AppendScriptOutput("── اقرأ السكربت كويس قبل ما تشغّله ──");
    }

    private void RefreshScriptMode()
    {
        if (!ScriptPermissions.IsSupported) { ScriptModeText = "غير مدعوم على ويندوز"; return; }
        if (!File.Exists(ScriptPath)) { ScriptModeText = "—"; return; }

        try
        {
            ScriptModeText = $"{ScriptPermissions.ToOctal(ScriptPath)}   {ScriptPermissions.ToSymbolic(ScriptPath)}"
                             + (ScriptPermissions.IsExecutable(ScriptPath) ? "   (قابل للتنفيذ)" : "   (مش قابل للتنفيذ)");
        }
        catch (Exception ex)
        {
            ScriptModeText = $"تعذّرت قراءة الصلاحيات: {ex.Message}";
        }
    }

    /// <summary>"+x" و "-x" و 755 و 644 و 700 — كل زرار بيبعت المعامل ده.</summary>
    [RelayCommand]
    private void ChangeMode(string? mode)
    {
        if (!HasScript || string.IsNullOrEmpty(mode)) return;

        try
        {
            ScriptPermissions.Apply(ScriptPath, mode);
            RefreshScriptMode();
            AppendScriptOutput($"$ chmod {mode} \"{Path.GetFileName(ScriptPath)}\"   →   {ScriptModeText}");
            AppendScriptOutput("   " + DescribeMode(mode));
        }
        catch (Exception ex)
        {
            AppendScriptOutput($"chmod فشل: {ex.Message}");
        }
    }

    /// <summary>شرح بالعربي لكل خيار chmod، بيظهر في المخرجات لما المستخدم يدوس على الزرار.</summary>
    private static string DescribeMode(string mode) => mode switch
    {
        "+x" => "+x  ←  بيضيف صلاحية التنفيذ للمالك والمجموعة والباقي، فتقدر تشغّل السكربت مباشرة (./script.sh). باقي الصلاحيات زي ما هي.",
        "-x" => "-x  ←  بيشيل صلاحية التنفيذ من الكل، فالملف يبقى للقراءة والكتابة بس ومينفعش يتشغّل مباشرة.",
        "755" => "755  ←  المالك: قراءة + كتابة + تنفيذ (7). المجموعة والباقي: قراءة + تنفيذ (5). الشائع للسكربتات والبرامج.",
        "644" => "644  ←  المالك: قراءة + كتابة (6). المجموعة والباقي: قراءة بس (4). ملف عادي مش قابل للتنفيذ.",
        "700" => "700  ←  المالك بس: قراءة + كتابة + تنفيذ. محدش غيره يقدر يقرأ الملف أو يشغّله (خصوصية كاملة).",
        _ => $"{mode}  ←  صلاحيات مخصّصة."
    };

    [RelayCommand(CanExecute = nameof(CanRunScript))]
    private Task RunScriptAsync() => RunScriptCoreAsync(makeExecutableFirst: false);

    [RelayCommand(CanExecute = nameof(CanRunScript))]
    private Task ChmodAndRunAsync() => RunScriptCoreAsync(makeExecutableFirst: true);

    [RelayCommand(CanExecute = nameof(IsScriptRunning))]
    private void CancelScript() => _scriptCts?.Cancel();

    [RelayCommand]
    private void ClearScriptOutput() => ScriptOutput = "";

    private bool CanRunScript() => HasScript && !IsScriptRunning;

    private async Task RunScriptCoreAsync(bool makeExecutableFirst)
    {
        if (!File.Exists(ScriptPath)) { AppendScriptOutput("الملف مش موجود."); return; }
        if (!ExternalTools.HasBash) { AppendScriptOutput("bash غير متاح على النظام ده."); return; }

        try
        {
            if (makeExecutableFirst && ScriptPermissions.IsSupported)
            {
                ScriptPermissions.Apply(ScriptPath, "+x");
                RefreshScriptMode();
                AppendScriptOutput($"$ chmod +x \"{Path.GetFileName(ScriptPath)}\"");
            }

            IsScriptRunning = true;
            _scriptCts = new CancellationTokenSource();

            AppendScriptOutput($"$ bash \"{ScriptPath}\" {ScriptArguments}".TrimEnd());

            var result = await BashScriptRunner.RunAsync(
                ScriptPath,
                SplitArguments(ScriptArguments),
                environment: null,
                workingDirectory: Path.GetDirectoryName(ScriptPath),
                onOutputLine: line => Dispatcher.UIThread.Post(() => AppendScriptOutput(line)),
                elevate: false,
                cancellationToken: _scriptCts.Token);

            AppendScriptOutput($"── انتهى بكود الخروج {result.ExitCode} ──");
        }
        catch (OperationCanceledException)
        {
            AppendScriptOutput("── اتوقّف ──");
        }
        catch (Exception ex)
        {
            AppendScriptOutput($"خطأ: {ex.Message}");
        }
        finally
        {
            _scriptCts?.Dispose();
            _scriptCts = null;
            IsScriptRunning = false;
        }
    }

    private void AppendScriptOutput(string line)
    {
        var text = ScriptOutput + line + "\n";
        if (text.Length > 200_000) text = text[^100_000..];
        ScriptOutput = text;
    }

    /// <summary>تقسيم بسيط للوسائط على المسافات مع دعم علامات الاقتباس المزدوجة.</summary>
    private static List<string> SplitArguments(string input)
    {
        var args = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var c in input)
        {
            if (c == '"') { inQuotes = !inQuotes; continue; }
            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (current.Length > 0) { args.Add(current.ToString()); current.Clear(); }
                continue;
            }
            current.Append(c);
        }

        if (current.Length > 0) args.Add(current.ToString());
        return args;
    }

    // ═════════════ تغييرات الخصائص ═════════════

    partial void OnSelectedFormatChanged(ArchiveFormatOption? value)
    {
        UpdateEncryptionChoices();
        OnPropertyChanged(nameof(DestinationPath));
    }

    partial void OnSelectedEncryptionChanged(EncryptionOption? value) =>
        OnPropertyChanged(nameof(UsePassword));

    partial void OnDestinationFolderChanged(string value) => OnPropertyChanged(nameof(DestinationPath));
    partial void OnArchiveFileNameChanged(string value) => OnPropertyChanged(nameof(DestinationPath));

    private void UpdateEncryptionChoices()
    {
        var list = new List<EncryptionOption> { new("بدون تشفير", EncryptionMethod.None) };

        if (SelectedFormat?.SupportsPassword == true)
        {
            list.Add(new EncryptionOption("AES-256  (موصى به)", EncryptionMethod.Aes256));
            if (SelectedFormat.Extension == ".zip")
                list.Add(new EncryptionOption("ZipCrypto  (قديم، للتوافق فقط)", EncryptionMethod.ZipCrypto));
        }

        var keep = SelectedEncryption?.Method;
        AvailableEncryptions = list;
        SelectedEncryption = list.FirstOrDefault(o => o.Method == keep) ?? list[0];
    }

    // ═════════════ أدوات مساعدة ═════════════

    private static string? NullIfEmpty(string s) => string.IsNullOrEmpty(s) ? null : s;

    /// <summary>بيشيل أي حاجة بعد أول نقطة وأي حروف غير صالحة في اسم ملف.</summary>
    private static string CleanFileName(string name)
    {
        var trimmed = name.Trim();
        var dot = trimmed.IndexOf('.');
        if (dot >= 0) trimmed = trimmed[..dot];

        var invalid = Path.GetInvalidFileNameChars();
        return new string(trimmed.Where(c => !invalid.Contains(c)).ToArray()).Trim();
    }

    private static string StripArchiveExtension(string path)
    {
        var name = Path.GetFileName(path);
        foreach (var ext in new[] { ".tar.gz", ".tar.bz2" })
            if (name.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                return name[..^ext.Length];

        return Path.GetFileNameWithoutExtension(name);
    }

    private static string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1) { value /= 1024; unit++; }
        return unit == 0 ? $"{bytes} B" : $"{value:F1} {units[unit]}";
    }
}
