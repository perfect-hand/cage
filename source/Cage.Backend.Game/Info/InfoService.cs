namespace Cage.Backend.Game.Info;

public class InfoService
{
    private string version;

    public InfoService() {
        // https://learn.microsoft.com/en-us/dotnet/standard/assembly/set-attributes-project-file
        version = typeof(InfoService).Assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .SingleOrDefault()?.InformationalVersion ?? "unknown";
    }

    public InfoDto GetInfo()
    {
        return new InfoDto
        {
            Version = version
        };
    }
}
