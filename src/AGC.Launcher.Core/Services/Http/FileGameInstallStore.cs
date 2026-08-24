using System.Text.Json;
using AGC.Launcher.Core.Models;

namespace AGC.Launcher.Core.Services.Http;

public sealed class FileGameInstallStore : IGameInstallStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AGC Launcher",
        "installed-games.json");

    public InstalledGameRecord? Find(string gameId) => LoadAll().GetValueOrDefault(gameId);

    public void Save(InstalledGameRecord record)
    {
        var all = LoadAll();
        all[record.GameId] = record;
        SaveAll(all);
    }

    public void Remove(string gameId)
    {
        var all = LoadAll();
        if (all.Remove(gameId))
        {
            SaveAll(all);
        }
    }

    private static Dictionary<string, InstalledGameRecord> LoadAll()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return [];
            }

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<Dictionary<string, InstalledGameRecord>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static void SaveAll(Dictionary<string, InstalledGameRecord> all)
    {
        var directory = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(all));
    }
}
