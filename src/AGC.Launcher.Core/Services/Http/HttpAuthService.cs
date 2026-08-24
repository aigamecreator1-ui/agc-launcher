using AGC.Shared.Dtos;

namespace AGC.Launcher.Core.Services.Http;

public sealed class HttpAuthService(ApiClient api) : IAuthService
{
    public Task<AuthResultDto> SignUpAsync(string email, string username, string password, CancellationToken ct = default)
        => api.PostAsync<AuthResultDto>("api/auth/signup", new SignUpRequestDto(email, username, password), ct);

    public Task<LoginRequestResponseDto> RequestLoginAsync(string email, CancellationToken ct = default)
        => api.PostAsync<LoginRequestResponseDto>("api/auth/login/request", new LoginRequestDto(email), ct);

    public Task<AuthResultDto> VerifyLoginCodeAsync(string email, string code, CancellationToken ct = default)
        => api.PostAsync<AuthResultDto>("api/auth/login/verify", new VerifyLoginCodeRequestDto(email, code), ct);

    public Task<AuthResultDto> VerifyOwnerCodeAsync(string email, string code, CancellationToken ct = default)
        => api.PostAsync<AuthResultDto>("api/auth/owner/verify", new VerifyOwnerCodeRequestDto(email, code), ct);
}
