using Cage.Backend.Editor.Attribute;
using Cage.Backend.Editor.Info;
using Cage.Backend.Editor.Organization;
using Cage.Backend.Editor.Title;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// https://learn.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-10.0&tabs=visual-studio-code
var builder = WebApplication.CreateBuilder(args);

// https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-10.0
var frontendUrl = Environment.GetEnvironmentVariable("CAGE_EDITOR_FRONTEND_URL") ?? "http://localhost:4200";

builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(
      policy =>
      {
        policy.WithOrigins(frontendUrl)
              .AllowAnyHeader()
              .AllowAnyMethod();
      });
});

// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/security?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
      options.Authority = "https://81875ece-550a-4647-8561-ef29633bfe62.ciamlogin.com/81875ece-550a-4647-8561-ef29633bfe62/v2.0"; // Tenant ID
      options.Audience = "a6791fa3-10da-4339-8097-ba31b7245e02"; // Backend application client ID

      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true
      };
    });
builder.Services.AddAuthorization();

// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/webapplication?view=aspnetcore-10.0#add-services
builder.Services.AddHttpLogging(options => { });
builder.Services.AddScoped<InfoService>();
builder.Services.AddScoped<OrganizationService>();
builder.Services.AddScoped<TitleService>();
builder.Services.AddScoped<AttributeService>();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseHttpLogging();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

var info = app.MapGroup("/info");
info.MapGet("/", GetInfo);

var organizations = app.MapGroup("/organizations").RequireAuthorization();
organizations.MapGet("/", GetAllOrganizations);
organizations.MapPost("/", CreateOrganization);
organizations.MapPut("/{organizationId}", UpdateOrganization);
organizations.MapDelete("/{organizationId}", DeleteOrganization);

organizations.MapGet("/{organizationId}/titles", GetAllTitles);
organizations.MapPost("/{organizationId}/titles", CreateTitle);
organizations.MapPut("/{organizationId}/titles/{titleId}", UpdateTitle);
organizations.MapDelete("/{organizationId}/titles/{titleId}", DeleteTitle);

organizations.MapGet("/{organizationId}/titles/{titleId}/attributes", GetAllAttributes);
organizations.MapPost("/{organizationId}/titles/{titleId}/attributes", CreateAttribute);
organizations.MapPut("/{organizationId}/titles/{titleId}/attributes/{attributeId}", UpdateAttribute);
organizations.MapDelete("/{organizationId}/titles/{titleId}/attributes/{attributeId}", DeleteAttribute);

static async Task<IResult> GetInfo(InfoService infoService)
{
  var info = infoService.GetInfo();
  return TypedResults.Ok(info);
}

static async Task<IResult> GetAllOrganizations(OrganizationService organizationService, HttpContext httpContext)
{
  var user = GetUser(httpContext);
  try
  {
    var organizations = await organizationService.ReadAll(user);
    return TypedResults.Ok(organizations);
  }
  catch (UnauthorizedAccessException)
  {
    return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
  }
}

static async Task<IResult> CreateOrganization(OrganizationService organizationService, OrganizationDto dto, HttpContext httpContext)
{
  var user = GetUser(httpContext);
  try
  {
    var organization = await organizationService.Create(dto, user);
    return TypedResults.Created($"/organizations/{organization.Id}", organization);
  }
  catch (UnauthorizedAccessException)
  {
    return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
  }
  catch (ArgumentException)
  {
    return TypedResults.BadRequest();
  }
}

static async Task<IResult> UpdateOrganization(OrganizationService organizationService, string organizationId, OrganizationDto dto, HttpContext httpContext)
{
  var user = GetUser(httpContext);
  try
  {
    return await organizationService.Update(organizationId, dto, user)
        ? TypedResults.NoContent()
        : TypedResults.NotFound();
  }
  catch (UnauthorizedAccessException)
  {
    return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
  }
  catch (ArgumentException)
  {
    return TypedResults.BadRequest();
  }
}

static async Task<IResult> DeleteOrganization(OrganizationService organizationService, string organizationId, HttpContext httpContext)
{
  var user = GetUser(httpContext);
  try
  {
    return await organizationService.Delete(organizationId, user)
        ? TypedResults.NoContent()
        : TypedResults.NotFound();
  }
  catch (UnauthorizedAccessException)
  {
    return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
  }
}

static async Task<IResult> GetAllTitles(TitleService titleService, string organizationId, HttpContext httpContext)
{
  var user = GetUser(httpContext);
  try
  {
    var titles = await titleService.ReadAll(organizationId, user);
    return TypedResults.Ok(titles);
  }
  catch (UnauthorizedAccessException)
  {
    return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
  }
}

static async Task<IResult> CreateTitle(TitleService titleService, string organizationId, TitleDto dto, HttpContext httpContext)
{
  var user = GetUser(httpContext);
  try
  {
    var title = await titleService.Create(organizationId, dto, user);
    return TypedResults.Created($"/organizations/{organizationId}/titles/{title.Id}", title);
  }
  catch (UnauthorizedAccessException)
  {
    return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
  }
  catch (ArgumentException)
  {
    return TypedResults.BadRequest();
  }
}

static async Task<IResult> UpdateTitle(TitleService titleService, string organizationId, string titleId, TitleDto dto, HttpContext httpContext)
{
  var user = GetUser(httpContext);
  try
  {
    return await titleService.Update(organizationId, titleId, dto, user)
        ? TypedResults.NoContent()
        : TypedResults.NotFound();
  }
  catch (UnauthorizedAccessException)
  {
    return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
  }
  catch (ArgumentException)
  {
    return TypedResults.BadRequest();
  }
}

static async Task<IResult> DeleteTitle(TitleService titleService, string organizationId, string titleId, HttpContext httpContext)
{
  var user = GetUser(httpContext);
  try
  {
    return await titleService.Delete(organizationId, titleId, user)
        ? TypedResults.NoContent()
        : TypedResults.NotFound();
  }
  catch (UnauthorizedAccessException)
  {
    return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
  }
}

static async Task<IResult> GetAllAttributes(AttributeService attributeService, string organizationId, string titleId, HttpContext httpContext)
{
  var user = GetUser(httpContext);
  try
  {
    var attributes = await attributeService.ReadAll(organizationId, titleId, user);
    return TypedResults.Ok(attributes);
  }
  catch (UnauthorizedAccessException)
  {
    return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
  }
}

static async Task<IResult> CreateAttribute(AttributeService attributeService, string organizationId, string titleId, AttributeDto dto, HttpContext httpContext)
{
  var user = GetUser(httpContext);
  try
  {
    var attribute = await attributeService.Create(organizationId, titleId, dto, user);
    return TypedResults.Created($"/organizations/{organizationId}/titles/{titleId}/attributes/{attribute.Id}", attribute);
  }
  catch (UnauthorizedAccessException)
  {
    return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
  }
  catch (ArgumentException)
  {
    return TypedResults.BadRequest();
  }
}

static async Task<IResult> UpdateAttribute(AttributeService attributeService, string organizationId, string titleId, string attributeId, AttributeDto dto, HttpContext httpContext)
{
  var user = GetUser(httpContext);
  try
  {
    return await attributeService.Update(organizationId, titleId, attributeId, dto, user)
        ? TypedResults.NoContent()
        : TypedResults.NotFound();
  }
  catch (UnauthorizedAccessException)
  {
    return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
  }
  catch (ArgumentException)
  {
    return TypedResults.BadRequest();
  }
}

static async Task<IResult> DeleteAttribute(AttributeService attributeService, string organizationId, string titleId, string attributeId, HttpContext httpContext)
{
  var user = GetUser(httpContext);
  try
  {
    return await attributeService.Delete(organizationId, titleId, attributeId, user)
        ? TypedResults.NoContent()
        : TypedResults.NotFound();
  }
  catch (UnauthorizedAccessException)
  {
    return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
  }
}

// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/use-http-context?view=aspnetcore-10.0
static string? GetUser(HttpContext httpContext)
{
  return httpContext.User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;
}

app.Run();
