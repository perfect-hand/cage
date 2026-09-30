using Cage.Simulation.Common;

namespace Cage.Simulation.Tests;

public class ToStringBuilderTests
{
    private class SampleType
    {
    }

    [Fact]
    public void BuildsRepresentationWithTypeNameAndFields()
    {
        var result = ToStringBuilder.For(new SampleType())
            .Add("Id", "1")
            .Add("Name", "Acme")
            .ToString();

        Assert.Equal("SampleType{Id=1, Name=Acme}", result);
    }

    [Fact]
    public void RendersNullValuesAsNullLiteral()
    {
        var result = ToStringBuilder.For(new SampleType())
            .Add("Id", null)
            .ToString();

        Assert.Equal("SampleType{Id=null}", result);
    }

    [Fact]
    public void BuildsEmptyRepresentationWithoutFields()
    {
        var result = ToStringBuilder.For(new SampleType()).ToString();

        Assert.Equal("SampleType{}", result);
    }

    [Fact]
    public void SupportsMixOfNullAndNonNullFields()
    {
        var result = ToStringBuilder.For(new SampleType())
            .Add("Id", null)
            .Add("Name", "Acme")
            .ToString();

        Assert.Equal("SampleType{Id=null, Name=Acme}", result);
    }
}
