namespace AGC.Launcher.Core.Services.Http;

public static class ApiConfig
{
    /// <summary>
    /// Debug builds (plain `dotnet run`) hit the local dev server so day-to-day
    /// development doesn't depend on the deployed one. Release builds — what the
    /// installer actually ships — hit the real deployed backend.
    /// </summary>
#if DEBUG
    public const string BaseUrl = "http://localhost:5137/";
#else
    public const string BaseUrl = "https://agc-launcher-server.onrender.com/";
#endif
}
