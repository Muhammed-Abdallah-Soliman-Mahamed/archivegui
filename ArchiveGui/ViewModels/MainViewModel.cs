using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Archive.Interface;
using Archive.Services;
using ArchiveGui.Models;

namespace ArchiveGui.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IArchiveService _archiveService = new SharpCompressArchiveService();

    // الصيغ المتاحة للإنشاء بس. 7z و RAR متعمدين مش موجودين هنا —
    // المستخدم أصلاً مش هيشوفهم في القايمة فمش هيقدر يحاول ينشئهم.
    public List<ArchiveFormatOption> AvailableFormats { get; } = new()
    {
        new ArchiveFormatOption("Zip (.zip)", ".zip"),
        new ArchiveFormatOption("Tar (.tar)", ".tar"),
        new ArchiveFormatOption("Tar + GZip (.tar.gz)", ".tar.gz"),
        new ArchiveFormatOption("Tar + BZip2 (.tar.bz2)", ".tar.bz2"),
    };

    [ObservableProperty]
    private ArchiveFormatOption _selectedFormat;

    [ObservableProperty]
    private string _selectedFilesText = "لم يتم اختيار ملفات بعد";

    [ObservableProperty]
    private string _destinationFolder = "/tmp";

    [ObservableProperty]
    private string _archiveFileName = "output";

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private string _statusMessage = "جاهز";

    [ObservableProperty]
    private string _extractionFolder = "/tmp/extracted-gui";

    [ObservableProperty]
    private string _sourceArchivePath = "";

    public List<string> SelectedFiles { get; set; } = new();

    public ObservableCollection<string> ArchiveEntries { get; } = new();

    // المسار الكامل بيتحسب تلقائيًا من (المجلد + الاسم النضيف + امتداد الصيغة المختارة)،
    // مش TextBox حر المستخدم يكتب فيه أي حاجة.
    public string DestinationPath =>
        Path.Combine(DestinationFolder, CleanFileName(ArchiveFileName) + SelectedFormat.Extension);

    public MainViewModel()
    {
        _selectedFormat = AvailableFormats[0];
    }

    // بيشيل أي نقطة (.) أو امتداد كتبه المستخدم بالغلط في خانة الاسم،
    // عشان الاسم النهائي يفضل نضيف واحد بس من الامتداد (بتاع الـ Dropdown).
    private static string CleanFileName(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return "archive";

        var firstDotIndex = rawName.IndexOf('.');
        var withoutExtension = firstDotIndex >= 0 ? rawName[..firstDotIndex] : rawName;

        return string.IsNullOrWhiteSpace(withoutExtension) ? "archive" : withoutExtension.Trim();
    }

    // لما أي حاجة من التلاتة دول تتغيّر، لازم نبلّغ الواجهة إن DestinationPath
    // (المحسوبة) اتغيّرت هي كمان، عشان الـ Binding يعرض القيمة الجديدة فورًا.
    partial void OnDestinationFolderChanged(string value) => OnPropertyChanged(nameof(DestinationPath));
    partial void OnArchiveFileNameChanged(string value) => OnPropertyChanged(nameof(DestinationPath));
    partial void OnSelectedFormatChanged(ArchiveFormatOption value) => OnPropertyChanged(nameof(DestinationPath));

    public void SetSelectedFiles(IReadOnlyList<string> files)
    {
        SelectedFiles = files.ToList();
        SelectedFilesText = SelectedFiles.Count == 0
            ? "لم يتم اختيار ملفات بعد"
            : $"تم اختيار {SelectedFiles.Count} ملف/ملفات";
    }

    public void SetDestinationFolder(string folderPath)
    {
        DestinationFolder = folderPath;
    }

    public void SetExtractionFolder(string folderPath)
    {
        ExtractionFolder = folderPath;
    }

    public void SetSourceArchivePath(string archivePath)
    {
        SourceArchivePath = archivePath;
    }

    [RelayCommand]
    private async Task CreateArchiveAsync()
    {
        if (SelectedFiles.Count == 0)
        {
            StatusMessage = "اختار ملفات الأول";
            return;
        }

        try
        {
            StatusMessage = "جاري إنشاء الأرشيف...";
            ProgressValue = 0;

            var progress = new Progress<double>(p => ProgressValue = p);
            await _archiveService.CreateAsync(SelectedFiles, DestinationPath, progress);

            SourceArchivePath = DestinationPath;
            StatusMessage = $"تم إنشاء الأرشيف بنجاح في: {DestinationPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ListEntriesAsync()
    {
        try
        {
            StatusMessage = "جاري قراءة محتويات الأرشيف...";
            ArchiveEntries.Clear();

            var entries = await _archiveService.ListEntriesAsync(SourceArchivePath);
            foreach (var entry in entries)
                ArchiveEntries.Add($"{entry.Path} - {entry.Size} bytes");

            StatusMessage = $"تم العثور على {entries.Count} عنصر";
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExtractArchiveAsync()
    {
        try
        {
            StatusMessage = "جاري الاستخراج...";
            ProgressValue = 0;

            var progress = new Progress<double>(p => ProgressValue = p);
            await _archiveService.ExtractAsync(SourceArchivePath, ExtractionFolder, progress);

            StatusMessage = $"تم الاستخراج في: {ExtractionFolder}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ: {ex.Message}";
        }
    }
}