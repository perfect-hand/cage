using Cage.Backend.Game.Common;

namespace Cage.Backend.Game.Info;

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
