using System.Net;
using System.Net.Http.Json;
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
  public async Task FailsToGetTagsWithoutAuthenticationOrFromForeignOrganization()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var foreignOrganizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, foreignOrganizationId, "Foreign");
    var foreignTitleId = await testDao.AddTitle(Guid.NewGuid().ToString(), "Title");
    var foreignVersionId = testDao.GetInitialVersionId(foreignTitleId);

    // WHEN
    var unauthorizedResponse = await unauthorizedHttpClient.GetAsync(TagsPath(organizationId, titleId, versionId));
    var foreignResponse = await httpClient.GetAsync(TagsPath(foreignOrganizationId, foreignTitleId, foreignVersionId));

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Forbidden, foreignResponse.StatusCode);
  }

  [Fact]
  public async Task CreatesTagAndRejectsInvalidOrExcessiveTags()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);

    // WHEN
    var createResponse = await httpClient.PostAsJsonAsync(TagsPath(organizationId, titleId, versionId), new TagDto { Name = "Monster" });
    var tooLongResponse = await httpClient.PostAsJsonAsync(
        TagsPath(organizationId, titleId, versionId),
        new TagDto { Name = new string('a', TagService.MaximumTagNameLength + 1) });
    var emptyResponse = await httpClient.PostAsJsonAsync(TagsPath(organizationId, titleId, versionId), new TagDto { Name = " " });

    for (var i = 1; i < TagService.MaximumTagsPerTitle; i++)
    {
      await testDao.AddTag(versionId, $"Tag{i}");
    }
    var excessResponse = await httpClient.PostAsJsonAsync(TagsPath(organizationId, titleId, versionId), new TagDto { Name = "Excess" });

    // THEN
    Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
    var created = await createResponse.Content.ReadFromJsonAsync<TagDto>();
    Assert.NotNull(created?.Id);
    Assert.Equal("Monster", created.Name);
    Assert.Equal(HttpStatusCode.BadRequest, tooLongResponse.StatusCode);
    Assert.Equal(HttpStatusCode.BadRequest, emptyResponse.StatusCode);
    Assert.Equal(HttpStatusCode.BadRequest, excessResponse.StatusCode);
  }

  [Fact]
  public async Task FailsToCreateTagsWithoutAuthenticationOrInForeignOrganization()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var foreignOrganizationId = Guid.NewGuid().ToString();
    await testDao.AddUserToOrganization(TestAuthHandler.TestUser, foreignOrganizationId, "Foreign");
    var foreignTitleId = await testDao.AddTitle(Guid.NewGuid().ToString(), "Title");
    var foreignVersionId = testDao.GetInitialVersionId(foreignTitleId);
    var dto = new TagDto { Name = "Resource" };

    // WHEN
    var unauthorizedResponse = await unauthorizedHttpClient.PostAsJsonAsync(TagsPath(organizationId, titleId, versionId), dto);
    var foreignResponse = await httpClient.PostAsJsonAsync(TagsPath(foreignOrganizationId, foreignTitleId, foreignVersionId), dto);

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Forbidden, foreignResponse.StatusCode);
    Assert.Empty(testDao.QueryTagsByTitleVersion(versionId));
    Assert.Empty(testDao.QueryTagsByTitleVersion(foreignVersionId));
  }

  [Fact]
  public async Task UpdatesTagAndRejectsInvalidOrMissingTags()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var tagId = await testDao.AddTag(versionId, "Old");

    // WHEN
    var updateResponse = await httpClient.PutAsJsonAsync($"{TagsPath(organizationId, titleId, versionId)}/{tagId}", new TagDto { Name = "New" });
    var tooLongResponse = await httpClient.PutAsJsonAsync(
        $"{TagsPath(organizationId, titleId, versionId)}/{tagId}",
        new TagDto { Name = new string('a', TagService.MaximumTagNameLength + 1) });
    var missingResponse = await httpClient.PutAsJsonAsync(
        $"{TagsPath(organizationId, titleId, versionId)}/{Guid.NewGuid()}",
        new TagDto { Name = "Missing" });

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
    Assert.Equal(HttpStatusCode.BadRequest, tooLongResponse.StatusCode);
    Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    Assert.Equal("New", Assert.Single(testDao.QueryTagsByTitleVersion(versionId)).Name);
  }

  [Fact]
  public async Task FailsToUpdateTagsWithoutAuthenticationOrInForeignOrganization()
  {
    // GIVEN
    var foreignOrganizationId = Guid.NewGuid().ToString();
    var foreignTitleId = await testDao.AddTitle(foreignOrganizationId, "Title");
    var foreignVersionId = testDao.GetInitialVersionId(foreignTitleId);
    var tagId = await testDao.AddTag(foreignVersionId, "Old");
    var dto = new TagDto { Name = "New" };

    // WHEN
    var unauthorizedResponse = await unauthorizedHttpClient.PutAsJsonAsync(
        $"{TagsPath(foreignOrganizationId, foreignTitleId, foreignVersionId)}/{tagId}",
        dto);
    var foreignResponse = await httpClient.PutAsJsonAsync(
        $"{TagsPath(foreignOrganizationId, foreignTitleId, foreignVersionId)}/{tagId}",
        dto);

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Forbidden, foreignResponse.StatusCode);
    Assert.Equal("Old", Assert.Single(testDao.QueryTagsByTitleVersion(foreignVersionId)).Name);
  }

  [Fact]
  public async Task DeletesTagAndRejectsUnauthenticatedOrForeignRequests()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var versionId = testDao.GetInitialVersionId(titleId);
    var tagId = await testDao.AddTag(versionId, "Resource");
    var foreignOrganizationId = Guid.NewGuid().ToString();
    var foreignTitleId = await testDao.AddTitle(foreignOrganizationId, "Title");
    var foreignVersionId = testDao.GetInitialVersionId(foreignTitleId);
    var foreignTagId = await testDao.AddTag(foreignVersionId, "Monster");

    // WHEN
    var unauthorizedResponse = await unauthorizedHttpClient.DeleteAsync($"{TagsPath(organizationId, titleId, versionId)}/{tagId}");
    var foreignResponse = await httpClient.DeleteAsync($"{TagsPath(foreignOrganizationId, foreignTitleId, foreignVersionId)}/{foreignTagId}");
    var deleteResponse = await httpClient.DeleteAsync($"{TagsPath(organizationId, titleId, versionId)}/{tagId}");

    // THEN
    Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Forbidden, foreignResponse.StatusCode);
    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    Assert.Empty(testDao.QueryTagsByTitleVersion(versionId));
    Assert.Single(testDao.QueryTagsByTitleVersion(foreignVersionId));
  }

  [Fact]
  public async Task TagsInChildVersionsRemainIndependentFromLaterParentChanges()
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
    var createAfterForkResponse = await httpClient.PostAsJsonAsync(
        TagsPath(organizationId, titleId, parentVersionId),
        new TagDto { Name = "Late" });
    var childResponse = await httpClient.GetAsync(TagsPath(organizationId, titleId, childVersionId));
    var parentResponse = await httpClient.GetAsync(TagsPath(organizationId, titleId, parentVersionId));

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
    Assert.Equal(HttpStatusCode.Created, createAfterForkResponse.StatusCode);
    var childTags = await childResponse.Content.ReadFromJsonAsync<List<TagDto>>();
    var parentTags = await parentResponse.Content.ReadFromJsonAsync<List<TagDto>>();
    Assert.Equal("Resource", Assert.Single(Assert.IsType<List<TagDto>>(childTags)).Name);
    Assert.Equal(new[] { "Late", "Monster" }, Assert.IsType<List<TagDto>>(parentTags).Select(tag => tag.Name).Order().ToArray());
  }

  [Fact]
  public async Task DeletesTagsWhenDeletingTitleOrTitleVersion()
  {
    // GIVEN
    var (organizationId, titleId) = await testDao.AddTitleToOwnOrganization();
    var rootVersionId = testDao.GetInitialVersionId(titleId);
    var childVersionId = await testDao.AddVersion(titleId, "2.0", rootVersionId);
    await testDao.AddTag(rootVersionId, "Resource");
    await testDao.AddTag(childVersionId, "Monster");

    // WHEN
    var deleteVersionResponse = await httpClient.DeleteAsync(
        $"/organizations/{organizationId}/titles/{titleId}/versions/{childVersionId}");
    var deleteTitleResponse = await httpClient.DeleteAsync($"/organizations/{organizationId}/titles/{titleId}");

    // THEN
    Assert.Equal(HttpStatusCode.NoContent, deleteVersionResponse.StatusCode);
    Assert.Empty(testDao.QueryTagsByTitleVersion(childVersionId));
    Assert.Equal(HttpStatusCode.NoContent, deleteTitleResponse.StatusCode);
    Assert.Empty(testDao.QueryTagsByTitleVersion(rootVersionId));
  }

  private static string TagsPath(string organizationId, string titleId, string versionId)
  {
    return $"/organizations/{organizationId}/titles/{titleId}/versions/{versionId}/tags";
  }
}
