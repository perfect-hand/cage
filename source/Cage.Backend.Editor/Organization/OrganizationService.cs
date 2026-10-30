using Azure.Data.Tables;
using Azure.Identity;
using Cage.Backend.Editor.Attribute;
using Cage.Backend.Editor.Title;

namespace Cage.Backend.Editor.Organization;

public class OrganizationService
{
  public const int MaximumOrganizationsPerUser = 10;
  public const int MaximumOrganizationNameLength = 50;

  private readonly OrganizationMapper mapper;
  private readonly OrganizationDao dao;
  private readonly TitleDao titleDao;
  private readonly AttributeDao attributeDao;
  private readonly ILogger<OrganizationService> logger;

  public OrganizationService(ILogger<OrganizationService> logger)
  {
    mapper = new OrganizationMapper();
    dao = new OrganizationDao();
    titleDao = new TitleDao();
    attributeDao = new AttributeDao();
    this.logger = logger;
  }

  public async Task<OrganizationDto> Create(OrganizationDto dto, string? user)
  {
    if (string.IsNullOrEmpty(user))
    {
      throw new UnauthorizedAccessException();
    }

    ValidateName(dto);

    if ((await dao.QueryByUser(user)).Count >= MaximumOrganizationsPerUser)
    {
      throw new ArgumentException($"Users cannot create more than {MaximumOrganizationsPerUser} organizations.", nameof(dto));
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

  public async Task<bool> Update(string organizationId, OrganizationDto dto, string? user)
  {
    await EnsureMembership(organizationId, user);
    ValidateName(dto);
    return await dao.UpdateOrganizationName(organizationId, dto.Name);
  }

  public async Task<bool> Delete(string organizationId, string? user)
  {
    await EnsureMembership(organizationId, user);
    var deleted = await dao.DeleteOrganization(organizationId);
    if (deleted)
    {
      foreach (var title in await titleDao.QueryByOrganization(organizationId))
      {
        await attributeDao.DeleteByTitle(title.RowKey);
      }

      await titleDao.DeleteByOrganization(organizationId);
      logger.LogInformation("Organization {} deleted.", organizationId);
    }

    return deleted;
  }

  private static void ValidateName(OrganizationDto dto)
  {
    if (dto.Name.Length > MaximumOrganizationNameLength)
    {
      throw new ArgumentException($"Organization names cannot exceed {MaximumOrganizationNameLength} characters.", nameof(dto));
    }
  }

  private async Task EnsureMembership(string organizationId, string? user)
  {
    if (string.IsNullOrEmpty(user))
    {
      throw new UnauthorizedAccessException();
    }

    if (!await dao.IsUserInOrganization(organizationId, user))
    {
      throw new UnauthorizedAccessException();
    }
  }
}
