using System.Net;
using System.Net.Http.Json;
using Cage.Backend.Game.Info;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cage.Backend.Game.Tests;

public class InfoEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient httpClient;

    public InfoEndpointTests(WebApplicationFactory<Program> factory)
    {
        httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task ReturnsApplicationVersion()
    {
        var response = await httpClient.GetAsync("/info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var info = await response.Content.ReadFromJsonAsync<InfoDto>();

        Assert.NotNull(info);
        Assert.NotNull(info.Version);
    }
}
