using Cage.Backend.Game.Info;
using Cage.Backend.Game.Match;

// https://learn.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-10.0&tabs=visual-studio-code
var builder = WebApplication.CreateBuilder(args);

// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/webapplication?view=aspnetcore-10.0#add-services
builder.Services.AddHttpLogging(options => {});
builder.Services.AddScoped<InfoService>();
builder.Services.AddScoped<MatchService>();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseHttpLogging();

var info = app.MapGroup("/info");
info.MapGet("/", GetInfo);

var matches = app.MapGroup("/matches");
matches.MapPost("/", CreateMatch);

static async Task<IResult> GetInfo(InfoService infoService)
{
    var info = infoService.GetInfo();
    return TypedResults.Ok(info);
}

static async Task<IResult> CreateMatch(MatchService matchService)
{
    var match = await matchService.CreateMatch();
    return TypedResults.Created($"/matches/{match.Id}", match);
}

app.Run();
