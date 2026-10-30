using Cage.Backend.Editor.Attribute;
using Cage.Backend.Editor.Organization;

namespace Cage.Backend.Editor.Title;

public class TitleService
{
  public const int MaximumTitlesPerOrganization = 100;
  public const int MaximumTitleNameLength = 50;

  private readonly TitleMapper mapper;
  private readonly TitleDao titleDao;
  private readonly AttributeDao attributeDao;
  private readonly OrganizationDao organizationDao;
  private readonly ILogger<TitleService> logger;

  public TitleService(ILogger<TitleService> logger)
  {
    mapper = new TitleMapper();
    titleDao = new TitleDao();
    attributeDao = new AttributeDao();
    organizationDao = new OrganizationDao();
    this.logger = logger;
  }

  public async Task<TitleDto> Create(string organizationId, TitleDto dto, string? user)
  {
    await EnsureMembership(organizationId, user);

    if (dto.Name.Length > MaximumTitleNameLength)
    {
      throw new ArgumentException($"Title names cannot exceed {MaximumTitleNameLength} characters.", nameof(dto));
    }

    if ((await titleDao.QueryByOrganization(organizationId)).Count >= MaximumTitlesPerOrganization)
    {
      throw new ArgumentException($"Organizations cannot have more than {MaximumTitlesPerOrganization} titles.", nameof(dto));
    }

    var title = mapper.ToEntity(dto, organizationId);
    await titleDao.AddTitle(title);

    logger.LogInformation("Title {} created in organization {}.", title.RowKey, organizationId);
    return mapper.ToDto(title);
  }

  public async Task<List<TitleDto>> ReadAll(string organizationId, string? user)
  {
    await EnsureMembership(organizationId, user);

    var titles = await titleDao.QueryByOrganization(organizationId);
    return titles.Select(mapper.ToDto).ToList();
  }

  public async Task<bool> Update(string organizationId, string titleId, TitleDto dto, string? user)
  {
    await EnsureMembership(organizationId, user);

    if (dto.Name.Length > MaximumTitleNameLength)
    {
      throw new ArgumentException($"Title names cannot exceed {MaximumTitleNameLength} characters.", nameof(dto));
    }

    return await titleDao.UpdateTitleName(organizationId, titleId, dto.Name);
  }

  public async Task<bool> Delete(string organizationId, string titleId, string? user)
  {
    await EnsureMembership(organizationId, user);
    var deleted = await titleDao.DeleteTitle(organizationId, titleId);
    if (deleted)
    {
      await attributeDao.DeleteByTitle(titleId);
    }

    return deleted;
  }

  private async Task EnsureMembership(string organizationId, string? user)
  {
    if (string.IsNullOrEmpty(user))
    {
      throw new UnauthorizedAccessException();
    }

    if (!await organizationDao.IsUserInOrganization(organizationId, user))
    {
      throw new UnauthorizedAccessException();
    }
  }
}
