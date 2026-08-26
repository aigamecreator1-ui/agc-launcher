namespace AGC.Shared.Dtos;

/// <summary>
/// LauncherOpens counts each account's first-ever launcher open, all-time — reopening
/// later never adds to it. RegisteredAccounts is the live count of accounts today, so
/// it can diverge from LauncherOpens if an account is later deleted.
/// </summary>
public sealed record LauncherAnalyticsDto(int LauncherOpens, int RegisteredAccounts);

public sealed record GameAnalyticsDto(
    string GameId,
    string Title,
    int Downloads,
    int Views,
    int Likes,
    int Dislikes,
    int CommentCount,
    DateTime? PublishedAt);

public sealed record AnalyticsOverviewDto(LauncherAnalyticsDto Launcher, IReadOnlyList<GameAnalyticsDto> Games);
