namespace AGC.Server.Services;

public interface IEmailSender
{
    Task SendVerificationCodeAsync(string toEmail, string code, CancellationToken ct = default);
}
