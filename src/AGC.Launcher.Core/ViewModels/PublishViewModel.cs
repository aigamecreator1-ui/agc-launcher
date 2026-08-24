using AGC.Launcher.Core.Services.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AGC.Launcher.ViewModels;

public sealed partial class PublishViewModel : ViewModelBase
{
    private readonly OwnerPublishService _publishService;

    public PublishViewModel(OwnerPublishService publishService)
    {
        _publishService = publishService;
    }

    public event EventHandler? Published;

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Genre { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Tags { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsPaid { get; set; }

    [ObservableProperty]
    public partial decimal PriceUsd { get; set; }

    [ObservableProperty]
    public partial string? BuildFilePath { get; set; }

    [ObservableProperty]
    public partial long BuildFileSizeBytes { get; set; }

    [ObservableProperty]
    public partial string? ThumbnailFilePath { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    public bool HasBuildFile => !string.IsNullOrEmpty(BuildFilePath);

    public bool HasThumbnail => !string.IsNullOrEmpty(ThumbnailFilePath);

    public string BuildFileDisplay => HasBuildFile
        ? $"{Path.GetFileName(BuildFilePath)} ({FormatBytes(BuildFileSizeBytes)})"
        : "No file selected";

    public string ThumbnailDisplay => HasThumbnail ? Path.GetFileName(ThumbnailFilePath)! : "No image selected";

    public async Task SetBuildFileAsync(string path, long sizeBytes)
    {
        BuildFilePath = path;
        BuildFileSizeBytes = sizeBytes;
        OnPropertyChanged(nameof(HasBuildFile));
        OnPropertyChanged(nameof(BuildFileDisplay));
        PublishCommand.NotifyCanExecuteChanged();

        if (IsPaid)
        {
            await SuggestPriceAsync();
        }
    }

    public void SetThumbnailFile(string path)
    {
        ThumbnailFilePath = path;
        OnPropertyChanged(nameof(HasThumbnail));
        OnPropertyChanged(nameof(ThumbnailDisplay));
        PublishCommand.NotifyCanExecuteChanged();
    }

    private async Task SuggestPriceAsync()
    {
        try
        {
            PriceUsd = await _publishService.SuggestPriceAsync(BuildFileSizeBytes);
        }
        catch
        {
            // Non-critical: the owner can still type a price manually.
        }
    }

    private bool CanPublish() =>
        !IsBusy
        && Title.Trim().Length > 0
        && Description.Trim().Length > 0
        && Genre.Trim().Length > 0
        && HasBuildFile
        && HasThumbnail;

    [RelayCommand(CanExecute = nameof(CanPublish))]
    private async Task PublishAsync()
    {
        ErrorMessage = null;
        StatusMessage = null;
        IsBusy = true;
        try
        {
            await _publishService.PublishAsync(
                Title.Trim(), Description.Trim(), Genre.Trim(), Tags.Trim(),
                IsPaid, IsPaid ? PriceUsd : 0, BuildFilePath!, ThumbnailFilePath!);

            StatusMessage = "Published. The launcher is closing briefly while the store updates.";
            Published?.Invoke(this, EventArgs.Empty);
            ResetForm();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetForm()
    {
        Title = string.Empty;
        Description = string.Empty;
        Genre = string.Empty;
        Tags = string.Empty;
        IsPaid = false;
        PriceUsd = 0;
        BuildFilePath = null;
        BuildFileSizeBytes = 0;
        ThumbnailFilePath = null;
        OnPropertyChanged(nameof(HasBuildFile));
        OnPropertyChanged(nameof(HasThumbnail));
        OnPropertyChanged(nameof(BuildFileDisplay));
        OnPropertyChanged(nameof(ThumbnailDisplay));
    }

    private static string FormatBytes(long bytes)
    {
        const long mb = 1024 * 1024;
        const long gb = 1024 * mb;
        return bytes >= gb ? $"{bytes / (double)gb:F2} GB" : $"{bytes / (double)mb:F1} MB";
    }

    partial void OnTitleChanged(string value) => PublishCommand.NotifyCanExecuteChanged();

    partial void OnDescriptionChanged(string value) => PublishCommand.NotifyCanExecuteChanged();

    partial void OnGenreChanged(string value) => PublishCommand.NotifyCanExecuteChanged();

    partial void OnIsBusyChanged(bool value) => PublishCommand.NotifyCanExecuteChanged();

    partial void OnIsPaidChanged(bool value)
    {
        if (value && HasBuildFile)
        {
            _ = SuggestPriceAsync();
        }
    }
}
