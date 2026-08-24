namespace AGC.Shared.Dtos;

public sealed record MaintenanceStatusDto(bool IsActive, string? Message, DateTime? ReopensAtUtc);
