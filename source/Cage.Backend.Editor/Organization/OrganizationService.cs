using Azure.Data.Tables;
using Azure.Identity;

namespace Cage.Backend.Editor.Organization;

public class OrganizationService
{
    private readonly OrganizationMapper mapper;
    private readonly ILogger<OrganizationService> logger;
    private readonly TableClient tableClient;

    public OrganizationService(ILogger<OrganizationService> logger)
    {
        mapper = new OrganizationMapper();
        this.logger = logger;
       
        // Use TABLE_STORAGE_URI to connect to Azure Table Storage in production.
        // Use TABLE_STORAGE_CONNECTION_STRING for connecting to Azurite in Testcontainers where ports can differ and HTTPS is not supported.
        // Fall back to default connection string for local development.
        // See https://learn.microsoft.com/en-us/azure/storage/common/storage-connect-azurite?tabs=table-storage
        var storageUri = Environment.GetEnvironmentVariable("TABLE_STORAGE_URI");
        var storageConnectionString = Environment.GetEnvironmentVariable("TABLE_STORAGE_CONNECTION_STRING") ?? "UseDevelopmentStorage=true";

        // See https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
        var tableServiceClient = storageUri != null
            ? new TableServiceClient(new Uri(storageUri), new DefaultAzureCredential())
            : new TableServiceClient(storageConnectionString);

        var tableName = "Organizations";
        tableServiceClient.CreateTableIfNotExists(tableName);

        tableClient = storageUri != null
            ? new TableClient(new Uri(storageUri), tableName, new DefaultAzureCredential())
            : new TableClient(storageConnectionString, tableName);
    }

    public async Task<OrganizationDto> Create(OrganizationDto dto)
    {
        var organization = mapper.ToEntity(dto);
        await tableClient.AddEntityAsync(organization);
        logger.LogInformation("Organization {} created.", organization.RowKey);
        return mapper.ToDto(organization);
    }

    public async Task<List<OrganizationDto>> ReadAll()
    {
       var organizations = tableClient.Query<OrganizationEntity>();
       return organizations.Select(mapper.ToDto).ToList();
    }
}
