using Cage.Backend.Editor.Info;
using Cage.Backend.Editor.Organization;

// https://learn.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-10.0&tabs=visual-studio-code
var builder = WebApplication.CreateBuilder(args);

// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/webapplication?view=aspnetcore-10.0#add-services
builder.Services.AddHttpLogging(options => {});
builder.Services.AddScoped<InfoService>();
builder.Services.AddScoped<OrganizationService>();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseHttpLogging();

var info = app.MapGroup("/info");
info.MapGet("/", GetInfo);

var organizations = app.MapGroup("/organizations");
organizations.MapGet("/", GetAllOrganizations);
organizations.MapPost("/", CreateOrganization);

static async Task<IResult> GetInfo(InfoService infoService)
{
    var info = infoService.GetInfo();
    return TypedResults.Ok(info);
}

static async Task<IResult> GetAllOrganizations(OrganizationService organizationService)
{
    var organizations = await organizationService.ReadAll();
    return TypedResults.Ok(organizations);
}

static async Task<IResult> CreateOrganization(OrganizationService organizationService, OrganizationDto dto)
{
    var organization = await organizationService.Create(dto);
    return TypedResults.Created($"/organizations/{organization.Id}", organization);
}

app.Run();
