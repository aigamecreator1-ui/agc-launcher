namespace AGC.Shared.Dtos;

public sealed record GameDto(
    string Id,
    string Title,
    string Description,
    string Genre,
    IReadOnlyList<string> Tags,
    bool IsPaid,
    decimal PriceUsd,
    long BuildSizeBytes,
    bool IsOwned);

public sealed record SuggestPriceRequestDto(long BuildSizeBytes);

public sealed record SuggestPriceResponseDto(decimal SuggestedPriceUsd);

public sealed record ClaimGameResponseDto(bool Success, string? ErrorMessage);

/// <summary>A published game as shown on the owner-only Games management screen.</summary>
public sealed record OwnerGameDto(
    string Id,
    string Title,
    bool IsPaid,
    decimal PriceUsd,
    long BuildSizeBytes,
    int OwnerCount,
    decimal TotalRevenueUsd,
    DateTime? PublishedAt);
