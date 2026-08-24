using AGC.Launcher.Core.Models;

namespace AGC.Launcher.Core.Services;

/// <summary>Persists the signed-in session locally so the app can resume without a fresh login.</summary>
public interface ISessionStore
{
    Session? Load();
    void Save(Session session);
    void Clear();
}
