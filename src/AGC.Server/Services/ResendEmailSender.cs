using System.Net.Http.Headers;
using System.Net.Http.Json;
using AGC.Server.Configuration;

namespace AGC.Server.Services;

public sealed class ResendEmailSender(HttpClient httpClient, AppOptions options) : IEmailSender
{
    public async Task SendVerificationCodeAsync(string toEmail, string code, CancellationToken ct = default)
    {
        httpClient.BaseAddress ??= new Uri("https://api.resend.com/");
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", options.ResendApiKey);

        var payload = new
        {
            from = options.ResendFromEmail,
            to = new[] { toEmail },
            subject = "Your AGC Launcher sign-in code",
            html = $"""
                <p>Your AGC Launcher sign-in code is:</p>
                <p style="font-size:28px;font-weight:700;letter-spacing:4px;">{code}</p>
                <p>This code expires in 10 minutes. If you didn't request this, you can ignore this email.</p>
                """,
        };

        var response = await httpClient.PostAsJsonAsync("emails", payload, ct);
        response.EnsureSuccessStatusCode();
    }
}
