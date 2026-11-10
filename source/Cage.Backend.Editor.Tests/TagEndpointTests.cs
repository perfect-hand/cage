using System.Net;
using System.Net.Http.Json;
using Cage.Backend.Editor.CardType;
using Cage.Backend.Editor.Tag;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cage.Backend.Editor.Tests;

public class TagEndpointTests : EndpointTest, IClassFixture<WebApplicationFactory<Program>>
{
  public TagEndpointTests(WebApplicationFactory<Program> factory) : base(factory)
  {
  }

  [Fact]
  public async Task GetsAllTagsOfTitleVersion()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var tagId = await testDao.AddTag(versionId, "Resource");
    await testDao.AddTag(Guid.NewGuid().ToString(), "Other");

    // WHEN
    var response = await httpClient.GetAsync(TagsPath(organizationId, titleId, versionId));

    // THEN
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var tags = await response.Content.ReadFromJsonAsync<List<TagDto>>();
    var tag = Assert.Single(Assert.IsType<List<TagDto>>(tags));
    Assert.Equal(tagId, tag.Id);
    Assert.Equal("Resource", tag.Name);
  }

  [Fact]
  public async Task FailsToGetTagsWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);

    // WHEN
    var response = await unauthorizedHttpClient.GetAsync(TagsPath(organizationId, titleId, versionId));

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task FailsToGetTagsOfTitleInForeignOrganization()
  {
    // GIVEN
    var organizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, organizationId, "Foreign");
    var titleId = await testDao.AddTitle(Guid.NewGuid().ToString(), "Title");
    var versionId = testDao.GetInitialVersionId(titleId);

    // WHEN
    var response = await httpClient.GetAsync(TagsPath(organizationId, titleId, versionId));

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task CreatesTag()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);

    // WHEN
    var response = await httpClient.PostAsJsonAsync(TagsPath(organizationId, titleId, versionId), new TagDto { Name = "Monster" });

    // THEN
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var created = await response.Content.ReadFromJsonAsync<TagDto>();
    Assert.NotNull(created?.Id);
    Assert.Equal("Monster", created.Name);
    var stored = Assert.Single(testDao.QueryTagsByTitleVersion(versionId));
    Assert.Equal(created.Id, stored.RowKey);
  }

  [Fact]
  public async Task FailsToCreateTagWithNameOverMaximumLength()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var dto = new TagDto { Name = new string('a', TagService.MaximumTagNameLength + 1) };

    // WHEN
    var response = await httpClient.PostAsJsonAsync(TagsPath(organizationId, titleId, versionId), dto);

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateTagWithWhitespaceName()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);

    // WHEN
    var response = await httpClient.PostAsJsonAsync(TagsPath(organizationId, titleId, versionId), new TagDto { Name = " " });

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateMoreThanMaximumTagsInTitle()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    for (var i = 0; i < TagService.MaximumTagsPerTitle; i++)
    {
      await testDao.AddTag(versionId, $"Tag{i}");
    }

    // WHEN
    var response = await httpClient.PostAsJsonAsync(TagsPath(organizationId, titleId, versionId), new TagDto { Name = "Excess" });

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateTagWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);

    // WHEN
    var response = await unauthorizedHttpClient.PostAsJsonAsync(TagsPath(organizationId, titleId, versionId), new TagDto { Name = "Resource" });

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Empty(testDao.QueryTagsByTitleVersion(versionId));
  }

  [Fact]
  public async Task FailsToCreateTagInForeignOrganization()
  {
    // GIVEN
    var foreignOrganizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, foreignOrganizationId, "Foreign");
    var foreignTitleId = await testDao.AddTitle(Guid.NewGuid().ToString(), "Title");
    var foreignVersionId = testDao.GetInitialVersionId(foreignTitleId);

    // WHEN
    var response = await httpClient.PostAsJsonAsync(
        TagsPath(foreignOrganizationId, foreignTitleId, foreignVersionId),
        new TagDto { Name = "Resource" });

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.Empty(testDao.QueryTagsByTitleVersion(foreignVersionId));
  }

  [Fact]
  public async Task UpdatesTag()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var tagId = await testDao.AddTag(versionId, "Old");

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"{TagsPath(organizationId, titleId, versionId)}/{tagId}",
        new TagDto { Name = "New" });

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Equal("New", Assert.Single(testDao.QueryTagsByTitleVersion(versionId)).Name);
  }

  [Fact]
  public async Task TagRenameUpdatesCardTypeReferencesAndPreservesChildVersion()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var parentVersionId = testDao.GetInitialVersionId(titleId);
    var tagId = await testDao.AddTag(parentVersionId, "Resource");
    await testDao.AddCardType(parentVersionId, "Monster", tags: ["Resource"]);
    var childVersionId = await testDao.AddVersion(titleId, "2.0", parentVersionId);

    // WHEN
    var updateResponse = await httpClient.PutAsJsonAsync(
        $"{TagsPath(organizationId, titleId, parentVersionId)}/{tagId}",
        new TagDto { Name = "Creature" });
    var parentCardTypesResponse = await httpClient.GetAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{parentVersionId}/card-types");
    var childCardTypesResponse = await httpClient.GetAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{childVersionId}/card-types");

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
    var parentCardTypes = await parentCardTypesResponse.Content.ReadFromJsonAsync<List<CardTypeDto>>();
    Assert.Contains("Creature", Assert.Single(Assert.IsType<List<CardTypeDto>>(parentCardTypes)).Tags);
    var childCardTypes = await childCardTypesResponse.Content.ReadFromJsonAsync<List<CardTypeDto>>();
    Assert.Contains("Resource", Assert.Single(Assert.IsType<List<CardTypeDto>>(childCardTypes)).Tags);
  }

  [Fact]
  public async Task ReportsTagCardTypeReferenceCount()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    await testDao.AddTag(versionId, "Resource");
    for (var i = 0; i < 2; i++)
    {
      await testDao.AddCardType(versionId, $"Monster{i}", tags: ["Resource"]);
    }

    // WHEN
    var response = await httpClient.GetAsync(TagsPath(organizationId, titleId, versionId));

    // THEN
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var tags = await response.Content.ReadFromJsonAsync<List<TagDto>>();
    Assert.Equal(2, Assert.Single(Assert.IsType<List<TagDto>>(tags)).ReferenceCount);
  }

  [Fact]
  public async Task FailsToUpdateTagWithNameOverMaximumLength()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var tagId = await testDao.AddTag(versionId, "Old");
    var dto = new TagDto { Name = new string('a', TagService.MaximumTagNameLength + 1) };

    // WHEN
    var response = await httpClient.PutAsJsonAsync($"{TagsPath(organizationId, titleId, versionId)}/{tagId}", dto);

    // THEN
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.Equal("Old", Assert.Single(testDao.QueryTagsByTitleVersion(versionId)).Name);
  }

  [Fact]
  public async Task FailsToUpdateMissingTag()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"{TagsPath(organizationId, titleId, versionId)}/{Guid.NewGuid()}",
        new TagDto { Name = "Missing" });

    // THEN
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task FailsToUpdateTagWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var tagId = await testDao.AddTag(versionId, "Old");

    // WHEN
    var response = await unauthorizedHttpClient.PutAsJsonAsync(
        $"{TagsPath(organizationId, titleId, versionId)}/{tagId}",
        new TagDto { Name = "New" });

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Equal("Old", Assert.Single(testDao.QueryTagsByTitleVersion(versionId)).Name);
  }

  [Fact]
  public async Task FailsToUpdateTagInForeignOrganization()
  {
    // GIVEN
    var foreignOrganizationId = Guid.NewGuid().ToString();
    var foreignTitleId = await testDao.AddTitle(foreignOrganizationId, "Title");
    var foreignVersionId = testDao.GetInitialVersionId(foreignTitleId);
    var tagId = await testDao.AddTag(foreignVersionId, "Old");

    // WHEN
    var response = await httpClient.PutAsJsonAsync(
        $"{TagsPath(foreignOrganizationId, foreignTitleId, foreignVersionId)}/{tagId}",
        new TagDto { Name = "New" });

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.Equal("Old", Assert.Single(testDao.QueryTagsByTitleVersion(foreignVersionId)).Name);
  }

  [Fact]
  public async Task DeletesTag()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var tagId = await testDao.AddTag(versionId, "Resource");

    // WHEN
    var response = await httpClient.DeleteAsync($"{TagsPath(organizationId, titleId, versionId)}/{tagId}");

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Empty(testDao.QueryTagsByTitleVersion(versionId));
  }

  [Fact]
  public async Task FailsToDeleteTagWithoutAuthentication()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var tagId = await testDao.AddTag(versionId, "Resource");

    // WHEN
    var response = await unauthorizedHttpClient.DeleteAsync($"{TagsPath(organizationId, titleId, versionId)}/{tagId}");

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.Single(testDao.QueryTagsByTitleVersion(versionId));
  }

  [Fact]
  public async Task FailsToDeleteTagInForeignOrganization()
  {
    // GIVEN
    var foreignOrganizationId = Guid.NewGuid().ToString();
    var foreignTitleId = await testDao.AddTitle(foreignOrganizationId, "Title");
    var foreignVersionId = testDao.GetInitialVersionId(foreignTitleId);
    var tagId = await testDao.AddTag(foreignVersionId, "Resource");

    // WHEN
    var response = await httpClient.DeleteAsync($"{TagsPath(foreignOrganizationId, foreignTitleId, foreignVersionId)}/{tagId}");

    // THEN
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    Assert.Single(testDao.QueryTagsByTitleVersion(foreignVersionId));
  }

  [Fact]
  public async Task ChildVersionKeepsParentTagValueWhenParentIsUpdated()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var parentVersionId = testDao.GetInitialVersionId(titleId);
    var createdResponse = await httpClient.PostAsJsonAsync(
        TagsPath(organizationId, titleId, parentVersionId),
        new TagDto { Name = "Resource" });
    var created = await createdResponse.Content.ReadFromJsonAsync<TagDto>();
    var tagId = Assert.IsType<string>(created?.Id);
    var childVersionId = await testDao.AddVersion(titleId, "2.0", parentVersionId);

    // WHEN
    var updateResponse = await httpClient.PutAsJsonAsync(
        $"{TagsPath(organizationId, titleId, parentVersionId)}/{tagId}",
        new TagDto { Name = "Monster" });
    var childResponse = await httpClient.GetAsync(TagsPath(organizationId, titleId, childVersionId));

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
    var childTags = await childResponse.Content.ReadFromJsonAsync<List<TagDto>>();
    Assert.Equal("Resource", Assert.Single(Assert.IsType<List<TagDto>>(childTags)).Name);
  }

  [Fact]
  public async Task ChildVersionKeepsParentTagWhenParentTagIsDeleted()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var parentVersionId = testDao.GetInitialVersionId(titleId);
    var tagId = await testDao.AddTag(parentVersionId, "Resource");
    var childVersionId = await testDao.AddVersion(titleId, "2.0", parentVersionId);

    // WHEN
    var deleteResponse = await httpClient.DeleteAsync(
        $"{TagsPath(organizationId, titleId, parentVersionId)}/{tagId}");
    var childResponse = await httpClient.GetAsync(TagsPath(organizationId, titleId, childVersionId));

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    var childTags = await childResponse.Content.ReadFromJsonAsync<List<TagDto>>();
    var childTag = Assert.Single(Assert.IsType<List<TagDto>>(childTags));
    Assert.Equal(tagId, childTag.Id);
    Assert.Equal("Resource", childTag.Name);
  }

  [Fact]
  public async Task ParentTagCreatedAfterForkIsHiddenFromChildVersion()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var parentVersionId = testDao.GetInitialVersionId(titleId);
    var childVersionId = await testDao.AddVersion(titleId, "2.0", parentVersionId);

    // WHEN
    var createResponse = await httpClient.PostAsJsonAsync(
        TagsPath(organizationId, titleId, parentVersionId),
        new TagDto { Name = "Late" });
    var childResponse = await httpClient.GetAsync(TagsPath(organizationId, titleId, childVersionId));

    // THEN
    Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
    var childTags = await childResponse.Content.ReadFromJsonAsync<List<TagDto>>();
    Assert.Empty(Assert.IsType<List<TagDto>>(childTags));
  }

  [Fact]
  public async Task DeletesTagsWhenDeletingTitleVersion()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var rootVersionId = testDao.GetInitialVersionId(titleId);
    var childVersionId = await testDao.AddVersion(titleId, "2.0", rootVersionId);
    await testDao.AddTag(childVersionId, "Monster");

    // WHEN
    var response = await httpClient.DeleteAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{childVersionId}");

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Empty(testDao.QueryTagsByTitleVersion(childVersionId));
  }

  [Fact]
  public async Task DeletesTagsWhenDeletingTitle()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    await testDao.AddTag(versionId, "Resource");

    // WHEN
    var response = await httpClient.DeleteAsync($"/organizations/{organizationId}/titles/{titleId}");

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.Empty(testDao.QueryTagsByTitleVersion(versionId));
  }

  private static string TagsPath(string organizationId, string titleId, string versionId)
  {
    return $"/organizations/{organizationId}/titles/{titleId}/versions/{versionId}/tags";
  }
}
