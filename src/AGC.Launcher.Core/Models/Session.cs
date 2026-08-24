using AGC.Shared.Dtos;

namespace AGC.Launcher.Core.Models;

public sealed class Session
{
    public required string Token { get; init; }
    public required UserDto User { get; init; }
}
