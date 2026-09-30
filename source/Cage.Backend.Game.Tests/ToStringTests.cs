using Cage.Backend.Game.Info;
using Cage.Backend.Game.Match;

namespace Cage.Backend.Game.Tests;

public class ToStringTests
{
    [Fact]
    public void MatchDtoToStringContainsAllFields()
    {
        var dto = new MatchDto { Id = "42" };

        Assert.Equal("MatchDto{Id=42}", dto.ToString());
    }

    [Fact]
    public void InfoDtoToStringContainsAllFields()
    {
        var dto = new InfoDto { Name = "Cage.Backend.Game", Version = "1.0.0" };

        Assert.Equal("InfoDto{Name=Cage.Backend.Game, Version=1.0.0}", dto.ToString());
    }
}
