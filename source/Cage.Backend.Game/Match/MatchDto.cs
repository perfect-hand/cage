using Cage.Backend.Game.Common;

namespace Cage.Backend.Game.Match;

public class MatchDto
{
    public required string Id { get; set; }

    public override string ToString()
    {
        return ToStringBuilder.For(this)
            .Add(nameof(Id), Id)
            .ToString();
    }
}
