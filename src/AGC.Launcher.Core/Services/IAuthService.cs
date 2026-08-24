using AGC.Launcher.Core.Models;
using AGC.Shared.Dtos;

namespace AGC.Launcher.Core.Services;

/// <summary>
/// Handles sign-up and sign-in. A real implementation talks to the backend, which is
/// the sole source of truth for accounts, verification codes, and the owner check —
/// nothing here should ever hold a long-lived password or decide who the owner is.
/// </summary>
public interface IAuthService
{
    Task<AuthResultDto> SignUpAsync(string email, string username, string password, CancellationToken ct = default);

    /// <summary>Returns which path the login flow should take next (email code / owner code / not found).</summary>
    Task<LoginRequestResponseDto> RequestLoginAsync(string email, CancellationToken ct = default);

    Task<AuthResultDto> VerifyLoginCodeAsync(string email, string code, CancellationToken ct = default);

    /// <summary>
    /// Throws ApiException with a generic message on any failure — wrong code, or the
    /// email not actually being the owner's — so callers can't distinguish the two.
    /// </summary>
    Task<AuthResultDto> VerifyOwnerCodeAsync(string email, string code, CancellationToken ct = default);
}
