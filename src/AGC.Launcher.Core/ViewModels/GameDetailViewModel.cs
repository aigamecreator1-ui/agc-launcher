using System.Collections.ObjectModel;
using System.ComponentModel;
using AGC.Launcher.Core.Services.Http;
using AGC.Shared.Dtos;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AGC.Launcher.ViewModels;

/// <summary>
/// Full detail page for a single game, reachable from either the Store or the
/// Library. Rather than re-implementing buy/install/play, it composes whichever
/// source item view model(s) are available and delegates straight to their
/// existing commands — so state (owned, installing, installed) always matches
/// what the originating list shows, with no separate copy to keep in sync.
/// </summary>
public sealed partial class GameDetailViewModel : ViewModelBase
{
    private readonly GameSocialService _socialService;
    private readonly Action _onBack;
    private readonly Action _onGoToLibrary;

    public GameDetailViewModel(
        GameDto game,
        StoreGameItemViewModel? storeItem,
        LibraryGameItemViewModel? libraryItem,
        GameSocialService socialService,
        Action onBack,
        Action onGoToLibrary)
    {
        Game = game;
        StoreItem = storeItem;
        LibraryItem = libraryItem;
        _socialService = socialService;
        _onBack = onBack;
        _onGoToLibrary = onGoToLibrary;

        Chips = new ObservableCollection<string>(BuildChips(game));
        RefreshScreenshots();

        if (storeItem is not null)
        {
            storeItem.PropertyChanged += OnStoreItemPropertyChanged;
        }

        if (libraryItem is not null)
        {
            libraryItem.PropertyChanged += OnLibraryItemPropertyChanged;
        }

        _ = LoadSocialAsync();
    }

    public GameDto Game { get; }

    public StoreGameItemViewModel? StoreItem { get; }

    public LibraryGameItemViewModel? LibraryItem { get; }

    public ObservableCollection<string> Chips { get; }

    public IReadOnlyList<Bitmap> Screenshots { get; private set; } = [];

    public bool HasScreenshot => Screenshots.Count > 0;

    public string PriceDisplay => Game.IsPaid ? $"${Game.PriceUsd:F2}" : "FREE";

    public string SizeDisplay => $"{Game.BuildSizeBytes / 1024d / 1024d:F0} MB";

    public bool IsOwned => LibraryItem is not null || (StoreItem?.IsOwned ?? Game.IsOwned);

    /// <summary>Buy is only offered when we reached this page from the Store and don't already own it.</summary>
    public bool ShowBuy => StoreItem is not null && !IsOwned;

    /// <summary>Install/Play are only offered when we reached this page from the Library.</summary>
    public bool ShowLibraryActions => LibraryItem is not null;

    /// <summary>Owned, but opened from the Store — nudge the player to the Library to install/play.</summary>
    public bool ShowGoToLibraryPrompt => IsOwned && LibraryItem is null;

    public ObservableCollection<GameCommentDto> Comments { get; } = [];

    [ObservableProperty]
    public partial int Likes { get; set; }

    [ObservableProperty]
    public partial int Dislikes { get; set; }

    /// <summary>null = no vote, true = liked, false = disliked.</summary>
    [ObservableProperty]
    public partial bool? UserVote { get; set; }

    [ObservableProperty]
    public partial bool IsSocialLoading { get; set; }

    [ObservableProperty]
    public partial bool IsPostingComment { get; set; }

    [ObservableProperty]
    public partial string? SocialErrorMessage { get; set; }

    [ObservableProperty]
    public partial string NewCommentText { get; set; } = "";

    public bool IsLiked => UserVote == true;

    public bool IsDisliked => UserVote == false;

    [RelayCommand]
    private void Back() => _onBack();

    [RelayCommand]
    private void GoToLibrary() => _onGoToLibrary();

    [RelayCommand]
    private async Task LikeAsync() => await VoteAsync(true);

    [RelayCommand]
    private async Task DislikeAsync() => await VoteAsync(false);

    private async Task VoteAsync(bool isLike)
    {
        SocialErrorMessage = null;
        try
        {
            ApplySocial(await _socialService.VoteAsync(Game.Id, isLike));
        }
        catch (Exception ex)
        {
            SocialErrorMessage = ex.Message;
        }
    }

    private bool CanPostComment() => !IsPostingComment && !string.IsNullOrWhiteSpace(NewCommentText);

    [RelayCommand(CanExecute = nameof(CanPostComment))]
    private async Task PostCommentAsync()
    {
        SocialErrorMessage = null;
        IsPostingComment = true;
        try
        {
            ApplySocial(await _socialService.PostCommentAsync(Game.Id, NewCommentText.Trim()));
            NewCommentText = "";
        }
        catch (Exception ex)
        {
            SocialErrorMessage = ex.Message;
        }
        finally
        {
            IsPostingComment = false;
        }
    }

    partial void OnNewCommentTextChanged(string value) => PostCommentCommand.NotifyCanExecuteChanged();

    partial void OnIsPostingCommentChanged(bool value) => PostCommentCommand.NotifyCanExecuteChanged();

    private async Task LoadSocialAsync()
    {
        IsSocialLoading = true;
        try
        {
            ApplySocial(await _socialService.RecordViewAsync(Game.Id));
        }
        catch (Exception ex)
        {
            SocialErrorMessage = ex.Message;
        }
        finally
        {
            IsSocialLoading = false;
        }
    }

    private void ApplySocial(GameSocialDto social)
    {
        Likes = social.Likes;
        Dislikes = social.Dislikes;
        UserVote = social.UserVote;
        OnPropertyChanged(nameof(IsLiked));
        OnPropertyChanged(nameof(IsDisliked));

        Comments.Clear();
        foreach (var comment in social.Comments)
        {
            Comments.Add(comment);
        }
    }

    private static IEnumerable<string> BuildChips(GameDto game)
    {
        if (!string.IsNullOrWhiteSpace(game.Genre))
        {
            yield return game.Genre;
        }

        foreach (var tag in game.Tags)
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                yield return tag;
            }
        }
    }

    private void RefreshScreenshots()
    {
        var thumbnail = LibraryItem?.Thumbnail ?? StoreItem?.Thumbnail;
        Screenshots = thumbnail is null ? [] : [thumbnail];
        OnPropertyChanged(nameof(Screenshots));
        OnPropertyChanged(nameof(HasScreenshot));
    }

    private void OnStoreItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(StoreGameItemViewModel.IsOwned):
                OnPropertyChanged(nameof(IsOwned));
                OnPropertyChanged(nameof(ShowBuy));
                OnPropertyChanged(nameof(ShowGoToLibraryPrompt));
                break;
            case nameof(StoreGameItemViewModel.Thumbnail):
                RefreshScreenshots();
                break;
        }
    }

    private void OnLibraryItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LibraryGameItemViewModel.Thumbnail))
        {
            RefreshScreenshots();
        }
    }
}
