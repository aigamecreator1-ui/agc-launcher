namespace AGC.Launcher.Core.Services.Http;

public static class ApiConfig
{
    /// <summary>
    /// Debug builds (plain `dotnet run`) hit the local dev server so day-to-day
    /// development doesn't depend on the deployed one. Release builds — what the
    /// installer actually ships — hit the real deployed backend.
    ///
    /// PLACEHOLDER: Koyeb assigns the real hostname (something like
    /// https://agc-launcher-server-&lt;your-org-slug&gt;.koyeb.app) only once the service
    /// has actually deployed — copy the exact URL from the service's Overview tab in
    /// the Koyeb dashboard and replace the line below with it.
    /// </summary>
#if DEBUG
    public const string BaseUrl = "http://localhost:5137/";
#else
    public const string BaseUrl = "https://agc-launcher-server.koyeb.app/";
#endif
}
