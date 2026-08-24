namespace AGC.Launcher.Core.Models;

public sealed record InstalledGameRecord(string GameId, string ExecutablePath, DateTime InstalledAtUtc);
