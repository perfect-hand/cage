using Cage.Simulation.Types;

namespace Cage.Backend.Editor.CardType;

public class CardTypeDto
{
  public string? Id { get; set; }
  public required string Name { get; set; }
  public string? ParentId { get; set; }
  public Dictionary<string, TypedValue> Attributes { get; set; } = new();
  public List<string> Tags { get; set; } = new();
}
