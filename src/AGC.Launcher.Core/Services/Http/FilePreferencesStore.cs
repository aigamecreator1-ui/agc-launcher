using System.Text.Json;
using AGC.Launcher.Core.Models;

namespace AGC.Launcher.Core.Services.Http;

public sealed class FilePreferencesStore : IPreferencesStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AGC Launcher",
        "preferences.json");

    public AppPreferences Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new AppPreferences();
            }

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AppPreferences>(json) ?? new AppPreferences();
        }
        catch
        {
            return new AppPreferences();
        }
    }

    public void Save(AppPreferences preferences)
    {
        var directory = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(preferences));
    }
}
