using Azure.Data.Tables;
using Azure.Identity;

namespace Cage.Backend.Editor.Organization;

public class OrganizationService
{
    private readonly OrganizationMapper mapper;
    private readonly OrganizationDao dao;
    private readonly ILogger<OrganizationService> logger;

    public OrganizationService(ILogger<OrganizationService> logger)
    {
        mapper = new OrganizationMapper();
        dao = new OrganizationDao();
        this.logger = logger;
    }

    public async Task<OrganizationDto> Create(OrganizationDto dto, string? user)
    {
        if (string.IsNullOrEmpty(user))
        {
            throw new UnauthorizedAccessException();
        }

        var organization = mapper.ToEntity(dto);
        await dao.AddOrganization(organization);

        var userInOrganization = new UserInOrganizationEntity
        {
            PartitionKey = organization.RowKey,
            RowKey = user,
            OrganizationId = organization.RowKey,
            OrganizationName = organization.Name
        };
        await dao.AddUserToOrganization(userInOrganization);
        userInOrganization = new UserInOrganizationEntity
        {
            PartitionKey = user,
            RowKey = organization.RowKey,
            OrganizationId = organization.RowKey,
            OrganizationName = organization.Name
        };
        await dao.AddUserToOrganization(userInOrganization);

        logger.LogInformation("Organization {} created.", organization.RowKey);
        return mapper.ToDto(organization);
    }

    public async Task<List<OrganizationDto>> ReadAll(string? user)
    {
        if (string.IsNullOrEmpty(user))
        {
            throw new UnauthorizedAccessException();
        }
        
       var usersInOrganizations = await dao.QueryByUser(user);
       return usersInOrganizations.Select(mapper.ToDto).ToList();
    }
}
