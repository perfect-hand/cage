using Cage.Simulation.Common;

namespace Cage.Backend.Editor.Title;

public class TitleDto
{
  public string? Id { get; set; }
  public required string Name { get; set; }

  public override string ToString()
  {
    return ToStringBuilder.For(this)
        .Add(nameof(Id), Id)
        .Add(nameof(Name), Name)
        .ToString();
  }
}
