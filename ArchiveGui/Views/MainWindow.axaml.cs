using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ArchiveGui.ViewModels;

namespace ArchiveGui.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnPickFilesClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "اختار الملفات",
            AllowMultiple = true
        });

        if (files.Count > 0 && DataContext is MainViewModel vm)
        {
            var paths = files.Select(f => f.Path.LocalPath).ToList();
            vm.SetSelectedFiles(paths);
        }
    }

    private async void OnPickFolderClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        // بيفتح نافذة اختيار مجلد بس (مش ملف) — بيحدد فيها المستخدم الـ Location
        // بصريًا بدل ما يكتب المسار يدوي.
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "اختار مجلد الحفظ",
            AllowMultiple = false
        });

        if (folders.Count > 0 && DataContext is MainViewModel vm)
        {
            vm.SetDestinationFolder(folders[0].Path.LocalPath);
        }
    }

    private async void OnPickExtractionFolderClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "اختار مجلد الاستخراج",
            AllowMultiple = false
        });

        if (folders.Count > 0 && DataContext is MainViewModel vm)
        {
            vm.SetExtractionFolder(folders[0].Path.LocalPath);
        }
    }

    private async void OnPickSourceArchiveClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "اختار ملف الأرشيف",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Archive Files")
                {
                    Patterns = new[] { "*.zip", "*.tar", "*.tar.gz", "*.tgz", "*.tar.bz2", "*.7z", "*.rar" }
                }
            }
        });

        if (files.Count > 0 && DataContext is MainViewModel vm)
        {
            vm.SetSourceArchivePath(files[0].Path.LocalPath);
        }
    }
}