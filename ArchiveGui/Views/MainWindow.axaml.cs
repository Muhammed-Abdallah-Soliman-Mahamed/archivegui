using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ArchiveGui.ViewModels;

namespace ArchiveGui.Views;

public partial class MainWindow : Window
{
    private static readonly FilePickerFileType ArchiveFiles = new("Archives")
    {
        Patterns = new[] { "*.zip", "*.tar", "*.tar.gz", "*.tgz", "*.tar.bz2", "*.7z", "*.rar" }
    };

    private static readonly FilePickerFileType ShellScripts = new("Shell scripts")
    {
        Patterns = new[] { "*.sh", "*.bash" }
    };

    public MainWindow()
    {
        InitializeComponent();

        // الـ ScriptOutput بيكبر وإحنا بنضيف عليه، فبننزّل الكيرسر لآخره عشان آخر سطر يبان.
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
                vm.PropertyChanged += OnViewModelPropertyChanged;
        };
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.ScriptOutput)) return;

        var box = this.FindControl<TextBox>("ScriptOutputBox");
        if (box != null) box.CaretIndex = box.Text?.Length ?? 0;
    }

    // ───────────── إنشاء: اختيار المصادر ─────────────

    private async void OnPickFilesClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "اختار الملفات",
            AllowMultiple = true
        });

        if (files.Count > 0)
            vm.AddSelectedPaths(files.Select(f => f.Path.LocalPath));
    }

    private async void OnPickSourceFolderClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "اختار مجلد للضغط",
            AllowMultiple = true
        });

        if (folders.Count > 0)
            vm.AddSelectedPaths(folders.Select(f => f.Path.LocalPath));
    }

    private async void OnPickDestinationFolderClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "اختار مجلد الحفظ",
            AllowMultiple = false
        });

        if (folders.Count > 0)
            vm.SetDestinationFolder(folders[0].Path.LocalPath);
    }

    // ───────────── استخراج ─────────────

    private async void OnPickSourceArchiveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "اختار ملف الأرشيف",
            AllowMultiple = false,
            FileTypeFilter = new[] { ArchiveFiles, FilePickerFileTypes.All }
        });

        if (files.Count > 0)
            vm.SetSourceArchive(files[0].Path.LocalPath);
    }

    private async void OnPickExtractionFolderClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "اختار مجلد الاستخراج",
            AllowMultiple = false
        });

        if (folders.Count > 0)
            vm.SetExtractionFolder(folders[0].Path.LocalPath);
    }

    // ───────────── سكربتات Bash ─────────────

    private async void OnPickScriptClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "اختار سكربت",
            AllowMultiple = false,
            FileTypeFilter = new[] { ShellScripts, FilePickerFileTypes.All }
        });

        if (files.Count > 0)
            vm.SetScript(files[0].Path.LocalPath);
    }
}
