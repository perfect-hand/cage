using Cage.Backend.Game.Match;

// https://learn.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-10.0&tabs=visual-studio-code
var builder = WebApplication.CreateBuilder(args);

// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/webapplication?view=aspnetcore-10.0#add-services
builder.Services.AddHttpLogging(options => {});
builder.Services.AddScoped<MatchService>();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseHttpLogging();

var matches = app.MapGroup("/matches");

matches.MapPost("/", CreateMatch);

static async Task<IResult> CreateMatch(MatchService matchService)
{
    var match = matchService.CreateMatch();
    return TypedResults.Created($"/matches/{match.Id}", match);
}

app.Run();
