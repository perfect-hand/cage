using Cage.Backend.Editor.Organization;

namespace Cage.Backend.Editor.Title;

public class TitleService
{
  private readonly TitleMapper mapper;
  private readonly TitleDao dao;
  private readonly OrganizationDao organizationDao;
  private readonly ILogger<TitleService> logger;

  public TitleService(ILogger<TitleService> logger)
  {
    mapper = new TitleMapper();
    dao = new TitleDao();
    organizationDao = new OrganizationDao();
    this.logger = logger;
  }

  public async Task<TitleDto> Create(string organizationId, TitleDto dto, string? user)
  {
    await EnsureMembership(organizationId, user);

    var title = mapper.ToEntity(dto, organizationId);
    await dao.AddTitle(title);

    logger.LogInformation("Title {} created in organization {}.", title.RowKey, organizationId);
    return mapper.ToDto(title);
  }

  public async Task<List<TitleDto>> ReadAll(string organizationId, string? user)
  {
    await EnsureMembership(organizationId, user);

    var titles = await dao.QueryByOrganization(organizationId);
    return titles.Select(mapper.ToDto).ToList();
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
