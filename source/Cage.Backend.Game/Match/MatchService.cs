namespace Cage.Backend.Game.Match;

public class MatchService(ILogger<MatchService> logger)
{
    internal MatchDto CreateMatch()
    {
        var match = new MatchDto
        {
            Id = Guid.NewGuid().ToString()
        };
        logger.LogInformation("Match {} created.", match.Id);
        return match;
    }
}
