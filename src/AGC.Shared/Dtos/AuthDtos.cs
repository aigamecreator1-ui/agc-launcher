namespace AGC.Shared.Dtos;

public sealed record SignUpRequestDto(string Email, string Username, string Password);

public sealed record LoginRequestDto(string Email);

public enum LoginRequestStatus
{
    EmailCodeSent,
    OwnerCodeRequired,
    AccountNotFound,
}

/// <summary>DevCode is only ever populated when the server has no email provider configured.</summary>
public sealed record LoginRequestResponseDto(LoginRequestStatus Status, string? DevCode = null);

public sealed record VerifyLoginCodeRequestDto(string Email, string Code);

public sealed record VerifyOwnerCodeRequestDto(string Email, string Code);

/// <summary>
/// Returned by every endpoint that establishes a session (sign-up, login-verify,
/// owner-verify). Failure paths return a plain error, never this shape — that's what
/// keeps a wrong owner code indistinguishable from any other failed login.
/// </summary>
public sealed record AuthResultDto(string Token, UserDto User);

public sealed record UserDto(string Id, string Email, string Username, bool IsOwner);
