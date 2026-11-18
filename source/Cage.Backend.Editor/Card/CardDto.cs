using Cage.Simulation.Types;

namespace Cage.Backend.Editor.Card;

public class CardDto
{
  public string? Id { get; set; }
  public required string Name { get; set; }
  public required string CardTypeId { get; set; }
  public Dictionary<string, TypedValue> Attributes { get; set; } = new();
  public List<string> Tags { get; set; } = new();
}
