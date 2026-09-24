using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Azure.Data.Tables;
using Cage.Backend.Editor.Organization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Azurite;

namespace Cage.Backend.Editor.Tests;

public class OrganizationEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly HttpClient unauthorizedHttpClient;
    private readonly HttpClient httpClient;

    // https://testcontainers.com/guides/getting-started-with-testcontainers-for-dotnet/
    // https://testcontainers.com/modules/azurite/?language=dotnet
    private readonly AzuriteContainer azuriteContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:3.37.0").Build();
    
    public async Task InitializeAsync()
    {
        await azuriteContainer.StartAsync();
        Environment.SetEnvironmentVariable("TABLE_STORAGE_CONNECTION_STRING", azuriteContainer.GetConnectionString());
    }

    public Task DisposeAsync()
    {
        return azuriteContainer.DisposeAsync().AsTask();
    }

    public OrganizationEndpointTests(WebApplicationFactory<Program> factory)
    {
        unauthorizedHttpClient = factory.CreateClient();

        // https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0&pivots=xunit#mock-authentication
        httpClient = factory.WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(services =>
                    {
                        services.AddAuthentication(options =>
                            {
                                options.DefaultAuthenticateScheme = "TestScheme";
                                options.DefaultChallengeScheme = "TestScheme";
                            })
                            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                                "TestScheme", options => { });
                    });
                })
                .CreateClient();

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(scheme: "TestScheme");
    }

    [Fact]
    public async Task GetsAllOrganizations()
    {
        // GIVEN
        var tableServiceClient = new TableServiceClient(azuriteContainer.GetConnectionString());

        var tableName = "Organizations";
        tableServiceClient.CreateTableIfNotExists(tableName);

        var tableClient = new TableClient(azuriteContainer.GetConnectionString(), tableName);
        await tableClient.AddEntityAsync(new OrganizationEntity
                {
                    PartitionKey = "Organization",
                    RowKey = Guid.NewGuid().ToString(),
                    Name = "Organization1"
                });
        await tableClient.AddEntityAsync(new OrganizationEntity
                {
                    PartitionKey = "Organization",
                    RowKey = Guid.NewGuid().ToString(),
                    Name = "Organization2"
                });

        // WHEN
        var response = await httpClient.GetAsync("/organizations");

        // THEN
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var organizations = await response.Content.ReadFromJsonAsync<List<OrganizationDto>>();

        Assert.NotNull(organizations);
        Assert.Equal(2, organizations.Count);
    }

    [Fact]
    public async Task FailsToGetAllOrganizationsWithoutAuthentication()
    {
        var response = await unauthorizedHttpClient.GetAsync("/organizations");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreatesOrganization()
    {
        // GIVEN
        var dto = new OrganizationDto
        {
            Name = "New Organization"
        };

        // WHEN
        var response = await httpClient.PostAsJsonAsync("/organizations", dto);

        // THEN
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdOrganization = await response.Content.ReadFromJsonAsync<OrganizationDto>();

        Assert.NotNull(createdOrganization);
        Assert.NotNull(createdOrganization.Id);
        Assert.Equal(dto.Name, createdOrganization.Name);
    }

    [Fact]
    public async Task FailsToCreateOrganizationsWithoutAuthentication()
    {
        // GIVEN
        var dto = new OrganizationDto
        {
            Name = "New Organization"
        };

        // WHEN
        var response = await unauthorizedHttpClient.PostAsJsonAsync("/organizations", dto);

        // THEN
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
