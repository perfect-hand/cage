using Cage.Backend.Editor.Organization;
using Cage.Backend.Editor.Title;

namespace Cage.Backend.Editor.Attribute;

public class AttributeService
{
  public const int MaximumAttributesPerTitle = 100;
  public const int MaximumAttributeNameLength = 50;

  private readonly AttributeMapper mapper;
  private readonly AttributeDao attributeDao;
  private readonly TitleDao titleDao;
  private readonly OrganizationDao organizationDao;
  private readonly ILogger<AttributeService> logger;

  public AttributeService(ILogger<AttributeService> logger)
  {
    mapper = new AttributeMapper();
    attributeDao = new AttributeDao();
    titleDao = new TitleDao();
    organizationDao = new OrganizationDao();
    this.logger = logger;
  }

  public async Task<AttributeDto> Create(string organizationId, string titleId, AttributeDto dto, string? user)
  {
    await EnsureAccess(organizationId, titleId, user);
    ValidateDto(dto);

    if ((await attributeDao.QueryByTitle(titleId)).Count >= MaximumAttributesPerTitle)
    {
      throw new ArgumentException($"Titles cannot have more than {MaximumAttributesPerTitle} attributes.", nameof(dto));
    }

    var attribute = mapper.ToEntity(dto, titleId);
    await attributeDao.AddAttribute(attribute);

    logger.LogInformation("Attribute {} created in title {}.", attribute.RowKey, titleId);
    return mapper.ToDto(attribute);
  }

  public async Task<List<AttributeDto>> ReadAll(string organizationId, string titleId, string? user)
  {
    await EnsureAccess(organizationId, titleId, user);

    var attributes = await attributeDao.QueryByTitle(titleId);
    return attributes.Select(mapper.ToDto).ToList();
  }

  public async Task<bool> Update(string organizationId, string titleId, string attributeId, AttributeDto dto, string? user)
  {
    await EnsureAccess(organizationId, titleId, user);
    ValidateDto(dto);

    return await attributeDao.UpdateAttribute(titleId, attributeId, dto.Name, dto.Type);
  }

  public async Task<bool> Delete(string organizationId, string titleId, string attributeId, string? user)
  {
    await EnsureAccess(organizationId, titleId, user);
    return await attributeDao.DeleteAttribute(titleId, attributeId);
  }

  private static void ValidateDto(AttributeDto dto)
  {
    if (dto.Name.Length > MaximumAttributeNameLength)
    {
      throw new ArgumentException($"Attribute names cannot exceed {MaximumAttributeNameLength} characters.", nameof(dto));
    }

    if (!Enum.IsDefined(dto.Type))
    {
      throw new ArgumentException("Attribute type is invalid.", nameof(dto));
    }
  }

  private async Task EnsureAccess(string organizationId, string titleId, string? user)
  {
    if (string.IsNullOrEmpty(user))
    {
      throw new UnauthorizedAccessException();
    }

    if (!await organizationDao.IsUserInOrganization(organizationId, user))
    {
      throw new UnauthorizedAccessException();
    }

    if (!await titleDao.TitleExists(organizationId, titleId))
    {
      throw new UnauthorizedAccessException();
    }
  }
}
