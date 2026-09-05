namespace Cage.Backend.Game.Match;

public class MatchService
{
    internal MatchDto CreateMatch()
    {
        return new MatchDto
        {
            Id = Guid.NewGuid().ToString()
        };
    }
}
