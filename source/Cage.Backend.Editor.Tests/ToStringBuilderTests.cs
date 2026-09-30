using Cage.Backend.Editor.Common;
using Cage.Backend.Editor.Info;
using Cage.Backend.Editor.Organization;

namespace Cage.Backend.Editor.Tests;

public class ToStringBuilderTests
{
    [Fact]
    public void BuildsRepresentationWithTypeNameAndFields()
    {
        var result = ToStringBuilder.For(new OrganizationDto { Id = "1", Name = "Acme" })
            .Add("Id", "1")
            .Add("Name", "Acme")
            .ToString();

        Assert.Equal("OrganizationDto{Id=1, Name=Acme}", result);
    }

    [Fact]
    public void RendersNullValuesAsNullLiteral()
    {
        var result = ToStringBuilder.For(new OrganizationDto { Id = null, Name = "Acme" })
            .Add("Id", null)
            .ToString();

        Assert.Equal("OrganizationDto{Id=null}", result);
    }

    [Fact]
    public void OrganizationEntityToStringContainsAllFields()
    {
        var entity = new OrganizationEntity
        {
            PartitionKey = "Organization",
            RowKey = "123",
            Name = "Acme"
        };

        Assert.Equal("OrganizationEntity{PartitionKey=Organization, RowKey=123, Name=Acme}", entity.ToString());
    }

    [Fact]
    public void UserInOrganizationEntityToStringContainsAllFields()
    {
        var entity = new UserInOrganizationEntity
        {
            PartitionKey = "user-1",
            RowKey = "org-1",
            OrganizationId = "org-1",
            OrganizationName = "Acme"
        };

        Assert.Equal(
            "UserInOrganizationEntity{PartitionKey=user-1, RowKey=org-1, OrganizationId=org-1, OrganizationName=Acme}",
            entity.ToString());
    }

    [Fact]
    public void OrganizationDtoToStringContainsAllFields()
    {
        var dto = new OrganizationDto { Id = "1", Name = "Acme" };

        Assert.Equal("OrganizationDto{Id=1, Name=Acme}", dto.ToString());
    }

    [Fact]
    public void InfoDtoToStringContainsAllFields()
    {
        var dto = new InfoDto { Name = "Cage.Backend.Editor", Version = "1.0.0" };

        Assert.Equal("InfoDto{Name=Cage.Backend.Editor, Version=1.0.0}", dto.ToString());
    }
}
