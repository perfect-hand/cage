using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cage.Backend.Game.Tests;

public class MatchEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient httpClient;

    public MatchEndpointTests(WebApplicationFactory<Program> factory)
    {
        httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task CreatesMatch()
    {
        var response = await httpClient.PostAsync("/matches", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var match = await response.Content.ReadFromJsonAsync<MatchDto>();

        Assert.NotNull(match);
        Assert.NotNull(match.Id);
    }
}
