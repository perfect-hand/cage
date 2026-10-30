using System.Text.Json.Serialization;
using Cage.Simulation.Common;
using Cage.Simulation.Types;

namespace Cage.Backend.Editor.Attribute;

public class AttributeDto
{
  public string? Id { get; set; }
  public required string Name { get; set; }

  [JsonConverter(typeof(JsonStringEnumConverter))]
  public CageType Type { get; set; }

  public override string ToString()
  {
    return ToStringBuilder.For(this)
        .Add(nameof(Id), Id)
        .Add(nameof(Name), Name)
        .Add(nameof(Type), Type)
        .ToString();
  }
}
