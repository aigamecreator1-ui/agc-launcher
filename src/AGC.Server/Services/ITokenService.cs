using AGC.Server.Entities;

namespace AGC.Server.Services;

public interface ITokenService
{
    string IssueToken(User user, bool isOwner);
}
