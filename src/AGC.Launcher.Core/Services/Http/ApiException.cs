namespace AGC.Launcher.Core.Services.Http;

/// <summary>The message is always safe to show directly to the user — it's whatever the server sent.</summary>
public sealed class ApiException(string message) : Exception(message);
