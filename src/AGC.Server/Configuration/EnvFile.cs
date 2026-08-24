namespace AGC.Server.Configuration;

/// <summary>
/// Loads the repo-root .env file into process environment variables at startup, since
/// ASP.NET Core has no built-in dotenv support. Searches upward from the current
/// directory so it works regardless of whether the server is launched from the repo
/// root or its own project folder. Real environment variables (e.g. set by a real
/// deployment) always win over .env values.
/// </summary>
public static class EnvFile
{
    public static void Load()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        string? envPath = null;

        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, ".env");
            if (File.Exists(candidate))
            {
                envPath = candidate;
                break;
            }

            dir = dir.Parent;
        }

        if (envPath is null)
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(envPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            {
                value = value[1..^1];
            }

            if (Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
