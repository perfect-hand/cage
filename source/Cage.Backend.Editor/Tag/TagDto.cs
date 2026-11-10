using Cage.Simulation.Common;

namespace Cage.Backend.Editor.Tag;

public class TagDto
{
  public string? Id { get; set; }
  public required string Name { get; set; }
  public int ReferenceCount { get; set; }

  public override string ToString()
  {
    return ToStringBuilder.For(this)
        .Add(nameof(Id), Id)
        .Add(nameof(Name), Name)
        .ToString();
  }
}
