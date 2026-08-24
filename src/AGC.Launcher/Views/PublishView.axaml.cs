using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AGC.Launcher.ViewModels;

namespace AGC.Launcher.Views;

public partial class PublishView : UserControl
{
    public PublishView()
    {
        InitializeComponent();
    }

    private async void OnChooseBuildFileClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PublishViewModel vm)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select build .zip",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Zip archive") { Patterns = ["*.zip"] }],
        });

        var file = files.FirstOrDefault();
        var path = file?.TryGetLocalPath();
        if (path is null)
        {
            return;
        }

        var props = await file!.GetBasicPropertiesAsync();
        await vm.SetBuildFileAsync(path, (long)(props.Size ?? 0));
    }

    private async void OnChooseThumbnailClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not PublishViewModel vm)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select thumbnail image",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Image") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp"] }],
        });

        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (path is null)
        {
            return;
        }

        vm.SetThumbnailFile(path);
    }
}
