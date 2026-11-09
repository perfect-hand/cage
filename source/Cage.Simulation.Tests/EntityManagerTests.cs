using Cage.Simulation.Entities;
using Cage.Simulation.Types;

namespace Cage.Simulation.Tests;

public class EntityManagerTests
{
  [Fact]
  public void CreatesEntityWithInheritedAndOverriddenBlueprintData()
  {
    var parent = new EntityBlueprint("Parent");
    parent.Attributes["Costs"] = new TypedValue(1);
    parent.Attributes["Power"] = new TypedValue(2);
    parent.Tags.Add("Monster");
    parent.Tags.Add("Common");

    var child = new EntityBlueprint("Child")
    {
      ParentId = parent.Id
    };
    child.Attributes["Power"] = new TypedValue(3);
    child.Tags.Add("Monster");
    child.Tags.Add("Flying");

    var registry = new EntityBlueprintRegistry();
    registry.Register(parent);
    registry.Register(child);

    var entity = new EntityManager(registry).CreateEntityFromBlueprint(child.Id);

    Assert.NotNull(entity);
    Assert.Equal(1, entity.Attributes["Costs"].AsInt());
    Assert.Equal(3, entity.Attributes["Power"].AsInt());
    Assert.Equal(new[] { "Monster", "Common", "Flying" }, entity.Tags);
  }

  [Fact]
  public void ThrowsWhenParentBlueprintIsMissing()
  {
    var blueprint = new EntityBlueprint("Child")
    {
      ParentId = "Missing"
    };
    var registry = new EntityBlueprintRegistry();
    registry.Register(blueprint);

    Assert.Throws<InvalidOperationException>(() => new EntityManager(registry).CreateEntityFromBlueprint(blueprint.Id));
  }

  [Fact]
  public void ThrowsWhenBlueprintHierarchyContainsCycle()
  {
    var first = new EntityBlueprint("First")
    {
      ParentId = "Second"
    };
    var second = new EntityBlueprint("Second")
    {
      ParentId = "First"
    };
    var registry = new EntityBlueprintRegistry();
    registry.Register(first);
    registry.Register(second);

    Assert.Throws<InvalidOperationException>(() => new EntityManager(registry).CreateEntityFromBlueprint(first.Id));
  }
}
