namespace AGC.Shared.Dtos;

public sealed record GameCommentDto(string Id, string Username, string Text, DateTime CreatedAt);

/// <summary>UserVote is null (no vote), true (liked), or false (disliked).</summary>
public sealed record GameSocialDto(int Likes, int Dislikes, bool? UserVote, IReadOnlyList<GameCommentDto> Comments);

public sealed record VoteRequestDto(bool IsLike);

public sealed record PostCommentRequestDto(string Text);

/// <summary>Empty acknowledgement body for endpoints with nothing else to return.</summary>
public sealed record AckDto;
