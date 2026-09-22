namespace Cage.Backend.Game.Match;

using Azure.Identity;
using Azure.Storage.Blobs;

public class MatchService(ILogger<MatchService> logger)
{
    public async Task<MatchDto> CreateMatch()
    {
        var match = new MatchDto
        {
            Id = Guid.NewGuid().ToString()
        };

        // Use BLOB_STORAGE_URI to connect to Azure Blob Storage in production.
        // Use BLOB_STORAGE_CONNECTION_STRING for connecting to Azurite in Testcontainers where ports can differ and HTTPS is not supported.
        // Fall back to default connection string for local development.
        // See https://learn.microsoft.com/en-us/azure/storage/common/storage-connect-azurite?tabs=blob-storage
        var storageUri = Environment.GetEnvironmentVariable("BLOB_STORAGE_URI");
        var storageConnectionString = Environment.GetEnvironmentVariable("BLOB_STORAGE_CONNECTION_STRING") ?? "UseDevelopmentStorage=true";

        // See https://learn.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet?tabs=net-cli%2Cmanaged-identity%2Croles-azure-portal%2Csign-in-azure-cli%2Cidentity-netcore-cli&pivots=blob-storage-quickstart-scratch
        var blobServiceClient = storageUri != null
            ? new BlobServiceClient(new Uri(storageUri), new DefaultAzureCredential())
            : new BlobServiceClient(storageConnectionString);

        var containerName = "matches-" + match.Id;
        var containerClient = await blobServiceClient.CreateBlobContainerAsync(containerName);
        var blobClient = containerClient.Value.GetBlobClient("match.json");
        await blobClient.UploadAsync(BinaryData.FromObjectAsJson(match), true);

        logger.LogInformation("Match {} created.", match.Id);
        return match;
    }
}
