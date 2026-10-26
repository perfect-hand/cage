using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Azurite;

namespace Cage.Backend.Editor.Tests;

[CollectionDefinition("Endpoint tests", DisableParallelization = true)]
public class EndpointTestCollection
{
}

public abstract class EndpointTest : IAsyncLifetime
{
  protected readonly HttpClient unauthorizedHttpClient;
  protected readonly HttpClient httpClient;
  protected EndpointTestDao testDao = null!;

  // https://testcontainers.com/guides/getting-started-with-testcontainers-for-dotnet/
  // https://testcontainers.com/modules/azurite/?language=dotnet
  protected readonly AzuriteContainer azuriteContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:3.37.0").Build();

  public virtual async Task InitializeAsync()
  {
    await azuriteContainer.StartAsync();
    Environment.SetEnvironmentVariable("CAGE_TABLE_STORAGE_CONNECTION_STRING", azuriteContainer.GetConnectionString());
    testDao = new EndpointTestDao(azuriteContainer.GetConnectionString());
  }

  public virtual Task DisposeAsync()
  {
    return azuriteContainer.DisposeAsync().AsTask();
  }

  protected EndpointTest(WebApplicationFactory<Program> factory)
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
}
