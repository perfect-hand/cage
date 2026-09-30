using Cage.Simulation.Common;

namespace Cage.Backend.Editor.Info;

public class InfoDto
{
    public required string Name { get; set; }
    public required string Version { get; set; }

    public override string ToString()
    {
        return ToStringBuilder.For(this)
            .Add(nameof(Name), Name)
            .Add(nameof(Version), Version)
            .ToString();
    }
}
