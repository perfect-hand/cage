using System.Net;
using System.Net.Http.Json;
using Cage.Backend.Editor.Attribute;
using Cage.Backend.Editor.Title;
using Cage.Simulation.Types;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cage.Backend.Editor.Tests;

public class TitleVersionEndpointTests : EndpointTest, IClassFixture<WebApplicationFactory<Program>>
{
  public TitleVersionEndpointTests(WebApplicationFactory<Program> factory) : base(factory)
  {
  }

  [Fact]
  public async Task ChangesToVersionAttributesDoNotAffectParentVersion()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var parentVersionId = testDao.GetInitialVersionId(titleId);
    var attributeId = await testDao.AddAttribute(parentVersionId, "Health", CageType.Int);
    var versionId = await testDao.AddVersion(titleId, "Draft", parentVersionId);

    // WHEN
    var updateResponse = await httpClient.PutAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{versionId}/attributes/{attributeId}",
        new AttributeDto { Name = "Health Points", Type = CageType.String });
    var createAttributeResponse = await httpClient.PostAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{versionId}/attributes",
        new AttributeDto { Name = "Armor", Type = CageType.Int });
    var deleteResponse = await httpClient.DeleteAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{versionId}/attributes/{attributeId}");

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Created, createAttributeResponse.StatusCode);
    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    var parentAttributes = Assert.Single(testDao.QueryAttributesByTitleVersion(parentVersionId));
    Assert.Equal(attributeId, parentAttributes.RowKey);
    Assert.Equal("Health", parentAttributes.Name);
    var childAttributesResponse = await httpClient.GetAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{versionId}/attributes");
    var childAttributes = await childAttributesResponse.Content.ReadFromJsonAsync<List<AttributeDto>>();
    Assert.Equal(HttpStatusCode.OK, childAttributesResponse.StatusCode);
    Assert.Equal("Armor", Assert.Single(Assert.IsType<List<AttributeDto>>(childAttributes)).Name);
  }

  [Fact]
  public async Task ChangesToParentAfterForkOnlyCopyChangedAttributesToChildren()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var parentVersionId = testDao.GetInitialVersionId(titleId);
    var attributeId = await testDao.AddAttribute(parentVersionId, "Health", CageType.Int);
    await testDao.AddAttribute(parentVersionId, "Mana", CageType.Int);
    var versionId = await testDao.AddVersion(titleId, "Draft", parentVersionId);
    await testDao.AddAttribute(versionId, "Armor", CageType.Int);
    var siblingVersionId = await testDao.AddVersion(titleId, "Sibling", parentVersionId);
    var nestedVersionId = await testDao.AddVersion(titleId, "Nested", versionId);

    // WHEN
    var updateResponse = await httpClient.PutAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{parentVersionId}/attributes/{attributeId}",
        new AttributeDto { Name = "Updated Health", Type = CageType.String });
    var childResponse = await httpClient.GetAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{versionId}/attributes");
    var childAttributes = await childResponse.Content.ReadFromJsonAsync<List<AttributeDto>>();
    var siblingResponse = await httpClient.GetAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{siblingVersionId}/attributes");
    var siblingAttributes = await siblingResponse.Content.ReadFromJsonAsync<List<AttributeDto>>();
    var nestedResponse = await httpClient.GetAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{nestedVersionId}/attributes");
    var nestedAttributes = await nestedResponse.Content.ReadFromJsonAsync<List<AttributeDto>>();

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
    Assert.Equal(HttpStatusCode.OK, childResponse.StatusCode);
    var childAttributeList = Assert.IsType<List<AttributeDto>>(childAttributes);
    Assert.Equal(3, childAttributeList.Count);
    var childAttribute = Assert.Single(childAttributeList, attribute => attribute.Id == attributeId);
    Assert.Equal(attributeId, childAttribute.Id);
    Assert.Equal("Health", childAttribute.Name);
    Assert.Equal(CageType.Int, childAttribute.Type);
    Assert.Equal(2, testDao.QueryAttributesByTitleVersion(versionId).Count);
    Assert.DoesNotContain(testDao.QueryAttributesByTitleVersion(versionId), attribute => attribute.Name == "Mana");
    Assert.Equal(HttpStatusCode.OK, siblingResponse.StatusCode);
    Assert.Equal("Health", Assert.Single(Assert.IsType<List<AttributeDto>>(siblingAttributes), attribute => attribute.Id == attributeId).Name);
    Assert.Single(testDao.QueryAttributesByTitleVersion(siblingVersionId));
    Assert.Equal(HttpStatusCode.OK, nestedResponse.StatusCode);
    Assert.Equal("Health", Assert.Single(Assert.IsType<List<AttributeDto>>(nestedAttributes), attribute => attribute.Id == attributeId).Name);
    Assert.Empty(testDao.QueryAttributesByTitleVersion(nestedVersionId));
  }

  [Fact]
  public async Task ParentAttributeCreatedAfterForkIsHiddenFromChildren()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var parentVersionId = testDao.GetInitialVersionId(titleId);
    var childVersionId = await testDao.AddVersion(titleId, "Draft", parentVersionId);
    var nestedVersionId = await testDao.AddVersion(titleId, "Nested", childVersionId);

    // WHEN
    var createResponse = await httpClient.PostAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{parentVersionId}/attributes",
        new AttributeDto { Name = "Health", Type = CageType.Int });
    var createdAttribute = await createResponse.Content.ReadFromJsonAsync<AttributeDto>();
    var childResponse = await httpClient.GetAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{childVersionId}/attributes");
    var childAttributes = await childResponse.Content.ReadFromJsonAsync<List<AttributeDto>>();
    var nestedResponse = await httpClient.GetAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{nestedVersionId}/attributes");
    var nestedAttributes = await nestedResponse.Content.ReadFromJsonAsync<List<AttributeDto>>();
    var parentResponse = await httpClient.GetAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{parentVersionId}/attributes");
    var parentAttributes = await parentResponse.Content.ReadFromJsonAsync<List<AttributeDto>>();

    // THEN
    Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
    Assert.NotNull(createdAttribute?.Id);
    Assert.Equal(HttpStatusCode.OK, childResponse.StatusCode);
    Assert.Empty(Assert.IsType<List<AttributeDto>>(childAttributes));
    Assert.True(Assert.Single(testDao.QueryAttributesByTitleVersion(childVersionId)).IsDeleted);
    Assert.Equal(HttpStatusCode.OK, nestedResponse.StatusCode);
    Assert.Empty(Assert.IsType<List<AttributeDto>>(nestedAttributes));
    Assert.Equal(HttpStatusCode.OK, parentResponse.StatusCode);
    Assert.Equal(createdAttribute.Id, Assert.Single(Assert.IsType<List<AttributeDto>>(parentAttributes)).Id);
  }

  [Fact]
  public async Task DeletingParentAttributeDoesNotAffectChildren()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var parentVersionId = testDao.GetInitialVersionId(titleId);
    var attributeId = await testDao.AddAttribute(parentVersionId, "Health", CageType.Int);
    await testDao.AddAttribute(parentVersionId, "Mana", CageType.Int);
    var childVersionId = await testDao.AddVersion(titleId, "Draft", parentVersionId);
    var nestedVersionId = await testDao.AddVersion(titleId, "Nested", childVersionId);

    // WHEN
    var deleteResponse = await httpClient.DeleteAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{parentVersionId}/attributes/{attributeId}");
    var childResponse = await httpClient.GetAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{childVersionId}/attributes");
    var childAttributes = await childResponse.Content.ReadFromJsonAsync<List<AttributeDto>>();
    var nestedResponse = await httpClient.GetAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{nestedVersionId}/attributes");
    var nestedAttributes = await nestedResponse.Content.ReadFromJsonAsync<List<AttributeDto>>();
    var parentResponse = await httpClient.GetAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{parentVersionId}/attributes");
    var parentAttributes = await parentResponse.Content.ReadFromJsonAsync<List<AttributeDto>>();

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    Assert.Equal(HttpStatusCode.OK, childResponse.StatusCode);
    Assert.Equal(attributeId, Assert.Single(Assert.IsType<List<AttributeDto>>(childAttributes), attribute => attribute.Id == attributeId).Id);
    Assert.Equal(HttpStatusCode.OK, nestedResponse.StatusCode);
    Assert.Equal(attributeId, Assert.Single(Assert.IsType<List<AttributeDto>>(nestedAttributes), attribute => attribute.Id == attributeId).Id);
    Assert.Equal(HttpStatusCode.OK, parentResponse.StatusCode);
    Assert.DoesNotContain(Assert.IsType<List<AttributeDto>>(parentAttributes), attribute => attribute.Id == attributeId);
  }

  [Fact]
  public async Task DeletingVersionDoesNotChangeOtherVersions()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var rootVersionId = testDao.GetInitialVersionId(titleId);
    await testDao.AddAttribute(rootVersionId, "Health", CageType.Int);
    var versionId = await testDao.AddVersion(titleId, "Draft", rootVersionId);
    await testDao.AddAttribute(versionId, "Armor", CageType.Int);
    var childVersionId = await testDao.AddVersion(titleId, "Next", versionId);

    // WHEN
    var deleteResponse = await httpClient.DeleteAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{versionId}");

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    var versionsResponse = await httpClient.GetAsync($"/organizations/{organizationId}/titles/{titleId}/versions");
    var versions = await versionsResponse.Content.ReadFromJsonAsync<List<TitleVersionDto>>();
    Assert.Contains(Assert.IsType<List<TitleVersionDto>>(versions), item => item.Id == childVersionId);
    var childAttributesResponse = await httpClient.GetAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{childVersionId}/attributes");
    var childAttributes = await childAttributesResponse.Content.ReadFromJsonAsync<List<AttributeDto>>();
    Assert.Equal(2, Assert.IsType<List<AttributeDto>>(childAttributes).Count);
    var childDeleteResponse = await httpClient.DeleteAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{childVersionId}");
    Assert.Equal(HttpStatusCode.NoContent, childDeleteResponse.StatusCode);
    var remainingVersionsResponse = await httpClient.GetAsync($"/organizations/{organizationId}/titles/{titleId}/versions");
    var remainingVersions = await remainingVersionsResponse.Content.ReadFromJsonAsync<List<TitleVersionDto>>();
    Assert.Single(Assert.IsType<List<TitleVersionDto>>(remainingVersions));
    var parentAttributesResponse = await httpClient.GetAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{rootVersionId}/attributes");
    var parentAttributes = await parentAttributesResponse.Content.ReadFromJsonAsync<List<AttributeDto>>();
    Assert.Equal("Health", Assert.Single(Assert.IsType<List<AttributeDto>>(parentAttributes)).Name);
  }

  [Fact]
  public async Task ListsTitleVersions()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var parentVersionId = testDao.GetInitialVersionId(titleId);
    var versionId = await testDao.AddVersion(titleId, "Draft", parentVersionId);

    // WHEN
    var response = await httpClient.GetAsync($"/organizations/{organizationId}/titles/{titleId}/versions");

    // THEN
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var versions = Assert.IsType<List<TitleVersionDto>>(await response.Content.ReadFromJsonAsync<List<TitleVersionDto>>());
    Assert.Contains(versions, item => item.Id == parentVersionId);
    Assert.Contains(versions, item => item.Id == versionId && item.Name == "Draft");
  }

  [Fact]
  public async Task FailsToListTitleVersionsWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();

    // WHEN
    var response = await unauthorizedHttpClient.GetAsync($"/organizations/{organizationId}/titles/{titleId}/versions");

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task FailsToListTitleVersionsInForeignOrganization()
  {
    // GIVEN
    var organizationId = Guid.NewGuid().ToString();
    var titleId = await testDao.AddTitle(Guid.NewGuid().ToString(), "Title");

    // WHEN
    var response = await httpClient.GetAsync($"/organizations/{organizationId}/titles/{titleId}/versions");

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task CreatesTitleVersion()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var parentVersionId = testDao.GetInitialVersionId(titleId);
    var dto = new TitleVersionDto { Name = "Draft", ParentVersionId = parentVersionId };

    // WHEN
    var response = await httpClient.PostAsJsonAsync($"/organizations/{organizationId}/titles/{titleId}/versions", dto);

    // THEN
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var createdVersion = Assert.IsType<TitleVersionDto>(await response.Content.ReadFromJsonAsync<TitleVersionDto>());
    Assert.NotNull(createdVersion.Id);
    Assert.Equal(dto.Name, createdVersion.Name);
    Assert.Equal(parentVersionId, createdVersion.ParentVersionId);
    Assert.Contains(testDao.QueryVersionsByTitle(titleId), version => version.RowKey == createdVersion.Id);
  }

  [Fact]
  public async Task FailsToCreateTitleVersionWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();

    // WHEN
    var response = await unauthorizedHttpClient.PostAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions",
        new TitleVersionDto { Name = "Draft" });

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateTitleVersionInForeignOrganization()
  {
    // GIVEN
    var organizationId = Guid.NewGuid().ToString();
    var titleId = await testDao.AddTitle(Guid.NewGuid().ToString(), "Title");

    // WHEN
    var response = await httpClient.PostAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions",
        new TitleVersionDto { Name = "Draft" });

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateTitleVersionWithNameOverMaximumLength()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var titleVersionName = new string('a', TitleVersionService.MaximumVersionNameLength + 1);

    // WHEN
    var response = await httpClient.PostAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions",
        new TitleVersionDto { Name = titleVersionName });

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateMoreThanMaximumTitleVersions()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    for (var i = 0; i < TitleVersionService.MaximumVersionsPerTitle - 1; i++)
    {
      await testDao.AddVersion(titleId, $"Version {i}");
    }

    // WHEN
    var response = await httpClient.PostAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions",
        new TitleVersionDto { Name = "One too many" });

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task RenamesTitleVersion()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = await testDao.AddVersion(titleId, "Draft", testDao.GetInitialVersionId(titleId));

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{versionId}",
        new TitleVersionDto { Name = "Release" });

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Equal("Release", Assert.Single(testDao.QueryVersionsByTitle(titleId), version => version.RowKey == versionId).Name);
  }

  [Fact]
  public async Task FailsToRenameTitleVersionWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = await testDao.AddVersion(titleId, "Draft");

    // WHEN
    var response = await unauthorizedHttpClient.PutAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{versionId}",
        new TitleVersionDto { Name = "Release" });

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Equal("Draft", Assert.Single(testDao.QueryVersionsByTitle(titleId), version => version.RowKey == versionId).Name);
  }

  [Fact]
  public async Task FailsToRenameTitleVersionInForeignOrganization()
  {
    // GIVEN
    var organizationId = Guid.NewGuid().ToString();
    var titleId = await testDao.AddTitle(Guid.NewGuid().ToString(), "Title");
    var versionId = testDao.GetInitialVersionId(titleId);

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{versionId}",
        new TitleVersionDto { Name = "Release" });

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.Equal(TitleVersionService.InitialVersionName, Assert.Single(testDao.QueryVersionsByTitle(titleId)).Name);
  }

  [Fact]
  public async Task FailsToRenameTitleVersionWithNameOverMaximumLength()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = await testDao.AddVersion(titleId, "Draft");

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{versionId}",
        new TitleVersionDto { Name = new string('a', TitleVersionService.MaximumVersionNameLength + 1) });

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal("Draft", Assert.Single(testDao.QueryVersionsByTitle(titleId), version => version.RowKey == versionId).Name);
  }

  [Fact]
  public async Task CannotDeleteTheOnlyVersionOfATitle()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();

    // WHEN
    var versionId = testDao.GetInitialVersionId(titleId);
    var response = await httpClient.DeleteAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{versionId}");

    // THEN
    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    var initialVersion = Assert.Single(testDao.QueryVersionsByTitle(titleId));
    Assert.Equal(TitleVersionService.InitialVersionName, initialVersion.Name);
    Assert.False(initialVersion.IsDeleted);
  }

  [Fact]
  public async Task FailsToDeleteTitleVersionWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = await testDao.AddVersion(titleId, "Draft");

    // WHEN
    var response = await unauthorizedHttpClient.DeleteAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{versionId}");

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Contains(testDao.QueryVersionsByTitle(titleId), version => version.RowKey == versionId);
  }

  [Fact]
  public async Task FailsToDeleteTitleVersionInForeignOrganization()
  {
    // GIVEN
    var organizationId = Guid.NewGuid().ToString();
    var titleId = await testDao.AddTitle(Guid.NewGuid().ToString(), "Title");
    var versionId = testDao.GetInitialVersionId(titleId);

    // WHEN
    var response = await httpClient.DeleteAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{versionId}");

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.Contains(testDao.QueryVersionsByTitle(titleId), version => version.RowKey == versionId);
  }

  [Fact]
  public async Task ReadOnlyVersionCannotBeChanged()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    await testDao.SetVersionReadOnly(titleId, versionId, true);

    // WHEN
    var rejectedResponse = await httpClient.PostAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{versionId}/attributes",
        new AttributeDto { Name = "Health", Type = CageType.Int });
    // THEN
    Assert.Equal(HttpStatusCode.Conflict, rejectedResponse.StatusCode);
  }

  [Fact]
  public async Task SetsTitleVersionReadOnly()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{versionId}/read-only",
        new TitleVersionReadOnlyDto { IsReadOnly = true });

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.True(Assert.Single(testDao.QueryVersionsByTitle(titleId)).IsReadOnly);
  }

  [Fact]
  public async Task ReadOnlyVersionCanBeUsedAsParent()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    await testDao.SetVersionReadOnly(titleId, versionId, true);

    // WHEN
    var response = await httpClient.PostAsJsonAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions",
        new TitleVersionDto { Name = "Next", ParentVersionId = versionId });
    var child = await response.Content.ReadFromJsonAsync<TitleVersionDto>();

    // THEN
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    Assert.Equal(versionId, child?.ParentVersionId);
  }
}
