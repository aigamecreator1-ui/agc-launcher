namespace AGC.Launcher.Core.Models;

public enum PurchaseOutcome
{
    Success,
    Cancelled,
    Failed,
}

public sealed class PurchaseResult
{
    public required PurchaseOutcome Outcome { get; init; }
    public string? ErrorMessage { get; init; }
}
