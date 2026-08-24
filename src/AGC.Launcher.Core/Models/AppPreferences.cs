namespace AGC.Launcher.Core.Models;

public sealed class AppPreferences
{
    public bool LaunchMinimized { get; set; }
    public bool NotifyOnNewRelease { get; set; } = true;
    public bool AutoInstallPurchases { get; set; }
}
