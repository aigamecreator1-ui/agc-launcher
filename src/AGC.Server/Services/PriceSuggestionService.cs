namespace AGC.Server.Services;

/// <summary>
/// A size-tiered heuristic, not real content analysis — there's no practical way to
/// assess a Unity build's quality/complexity from the launcher. The owner can always
/// override before publishing.
/// </summary>
public static class PriceSuggestionService
{
    public static decimal SuggestPrice(long buildSizeBytes)
    {
        const long mb = 1024 * 1024;
        const long gb = 1024 * mb;

        return buildSizeBytes switch
        {
            < 50 * mb => 2.99m,
            < 200 * mb => 4.99m,
            < 500 * mb => 7.99m,
            < gb + gb / 2 => 12.99m, // < 1.5 GB
            < 4 * gb => 19.99m,
            _ => 24.99m,
        };
    }
}
