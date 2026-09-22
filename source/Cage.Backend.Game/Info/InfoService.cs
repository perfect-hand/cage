namespace Cage.Backend.Game.Info;

public class InfoService
{
    private string name;
    private string version;

    public InfoService() {
        // https://learn.microsoft.com/en-us/dotnet/standard/assembly/set-attributes-project-file
        name = typeof(InfoService).Assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyProductAttribute), false)
            .OfType<System.Reflection.AssemblyProductAttribute>()
            .SingleOrDefault()?.Product ?? "unknown";
        version = typeof(InfoService).Assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .SingleOrDefault()?.InformationalVersion ?? "unknown";
    }

    public InfoDto GetInfo()
    {
        return new InfoDto
        {
            Name = name,
            Version = version
        };
    }
}
