using System.Text.Json;
using AGC.Launcher.Core.Models;

namespace AGC.Launcher.Core.Services.Http;

public sealed class FileSessionStore : ISessionStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AGC Launcher",
        "session.json");

    public Session? Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<Session>(json);
        }
        catch
        {
            return null;
        }
    }

    public void Save(Session session)
    {
        var directory = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(session));
    }

    public void Clear()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }
    }
}
