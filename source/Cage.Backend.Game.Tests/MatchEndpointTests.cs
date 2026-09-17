using System.Net;
using System.Net.Http.Json;
using Cage.Backend.Game.Match;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.Azurite;

namespace Cage.Backend.Game.Tests;

public class MatchEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly HttpClient httpClient;

    private readonly AzuriteContainer azuriteContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:3.37.0").Build();

    public async Task InitializeAsync()
    {
        await azuriteContainer.StartAsync();
        Environment.SetEnvironmentVariable("BLOB_STORAGE_CONNECTION_STRING", azuriteContainer.GetConnectionString());
    }

    public Task DisposeAsync()
    {
        return azuriteContainer.DisposeAsync().AsTask();
    }

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
