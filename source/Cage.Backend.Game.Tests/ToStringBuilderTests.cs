using Cage.Backend.Game.Common;
using Cage.Backend.Game.Info;
using Cage.Backend.Game.Match;

namespace Cage.Backend.Game.Tests;

public class ToStringBuilderTests
{
    [Fact]
    public void BuildsRepresentationWithTypeNameAndFields()
    {
        var result = ToStringBuilder.For(new MatchDto { Id = "1" })
            .Add("Id", "1")
            .ToString();

        Assert.Equal("MatchDto{Id=1}", result);
    }

    [Fact]
    public void RendersNullValuesAsNullLiteral()
    {
        var result = ToStringBuilder.For(new MatchDto { Id = "1" })
            .Add("Value", null)
            .ToString();

        Assert.Equal("MatchDto{Value=null}", result);
    }

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
