using AGC.Shared.Dtos;

namespace AGC.Launcher.Core.Services.Http;

/// <summary>Likes/dislikes and comments on a game's detail page.</summary>
public sealed class GameSocialService(ApiClient api)
{
    public Task<GameSocialDto> RecordViewAsync(string gameId, CancellationToken ct = default)
        => api.PostAsync<GameSocialDto>($"api/games/{gameId}/view", new { }, ct);

    public Task<GameSocialDto> GetSocialAsync(string gameId, CancellationToken ct = default)
        => api.GetAsync<GameSocialDto>($"api/games/{gameId}/social", ct);

    public Task<GameSocialDto> VoteAsync(string gameId, bool isLike, CancellationToken ct = default)
        => api.PostAsync<GameSocialDto>($"api/games/{gameId}/vote", new VoteRequestDto(isLike), ct);

    public Task<GameSocialDto> PostCommentAsync(string gameId, string text, CancellationToken ct = default)
        => api.PostAsync<GameSocialDto>($"api/games/{gameId}/comments", new PostCommentRequestDto(text), ct);
}
