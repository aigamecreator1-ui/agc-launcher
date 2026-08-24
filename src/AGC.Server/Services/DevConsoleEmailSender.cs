namespace AGC.Server.Services;

/// <summary>
/// Used when RESEND_API_KEY isn't configured. Logs the code instead of emailing it;
/// AuthController separately echoes the code back in the API response in this mode so
/// the login flow stays testable without a Resend account.
/// </summary>
public sealed class DevConsoleEmailSender(ILogger<DevConsoleEmailSender> logger) : IEmailSender
{
    public Task SendVerificationCodeAsync(string toEmail, string code, CancellationToken ct = default)
    {
        logger.LogWarning(
            "[DEV MODE — RESEND_API_KEY not set] Verification code for {Email}: {Code}", toEmail, code);
        return Task.CompletedTask;
    }
}
