
using System.Linq.Expressions;
using Cage.Simulation;
using Cage.Simulation.Entities;
using Cage.Simulation.Events;
using Cage.Simulation.Expressions;
using Cage.Simulation.Functions;
using Cage.Simulation.Mutations;
using Cage.Simulation.Types;

namespace Cage.Simulation.Tests;

public class MatchTests
{
    [Fact]
    public void InitializesMatch()
    {
        // Setup data.
        var resourceCard = new EntityBlueprint("Resource");
        resourceCard.Tags.Add("Resource");

        var monsterCard = new EntityBlueprint("Monster");
        monsterCard.Tags.Add("Monster");
        monsterCard.Attributes["Costs"] = new TypedValue(1);
        monsterCard.Attributes["Power"] = new TypedValue(1);

        var blueprintRegistry = new EntityBlueprintRegistry();
        blueprintRegistry.Register(resourceCard);
        blueprintRegistry.Register(monsterCard);

        var entityManager = new EntityManager(blueprintRegistry);

        // Create match players.
        var match = new Match(entityManager);

        var deckCardIds = new List<string> { "Resource", "Resource", "Resource", "Resource", "Monster", "Monster", "Monster", "Monster" };

        var player1 = match.EntityManager.CreateEntity();
        player1.Tags.Add("Player");
        var player1Deck = entityManager.CreateEntitiesFromBlueprintIds(deckCardIds);
        player1.Attributes["Deck"] = new TypedValue(player1Deck.ToList());
        player1.Attributes["Hand"] = new TypedValue(new List<Entity>());

        var player2 = match.EntityManager.CreateEntity();
        player2.Tags.Add("Player");
        var player2Deck = entityManager.CreateEntitiesFromBlueprintIds(deckCardIds);
        player2.Attributes["Deck"] = new TypedValue(player2Deck.ToList());
        player2.Attributes["Hand"] = new TypedValue(new List<Entity>());

        // Build trigger for match initialization.
        var matchInitializationTrigger = new Trigger
        {
            EventName = typeof(MatchInitializationEvent).Name,
            Actions =
            {
                new ForEachEntityMutation
                {
                    LoopVariable = "player",
                    EntityList = new FunctionExpression
                    {
                        Function = new FindEntitiesByTagFunction
                        {
                            Tag = new LiteralExpression
                            {
                                Value = new TypedValue("Player")
                            }
                        }
                    },
                    Mutations =
                    {
                        new ShuffleEntityListMutation
                        {
                            Entity = new VariableExpression
                            {
                                Name = "player"
                            },
                            Attribute = "Deck"
                        },
                        new RepeatMutation
                        {
                            Times = new LiteralExpression
                            {
                                Value = new TypedValue(3)
                            },
                            Mutations =
                            {
                                new MoveEntityMutation
                                {
                                    EntityToMove = new FunctionExpression
                                    {
                                        Function = new EntityAtIndexFunction
                                        {
                                            EntityList = new FunctionExpression
                                            {
                                                Function = new EntityAttributeValueFunction
                                                {
                                                    Entity = new VariableExpression
                                                    {
                                                        Name = "player"
                                                    },
                                                    AttributeName = new LiteralExpression
                                                    {
                                                        Value = new TypedValue("Deck")
                                                    }
                                                }
                                            },
                                            ListIndex = new LiteralExpression
                                            {
                                                Value = new TypedValue(0)
                                            }
                                        }
                                    },
                                    SourceEntity = new VariableExpression
                                    {
                                        Name = "player"
                                    },
                                    SourceAttribute = "Deck",
                                    TargetEntity = new VariableExpression
                                    {
                                        Name = "player"
                                    },
                                    TargetAttribute = "Hand"
                                }
                            }
                        },
                        new SetIntegerAttributeMutation
                        {
                            Entity = new VariableExpression
                            {
                                Name = "player"
                            },
                            Attribute = "Health",
                            NewValue = new LiteralExpression
                            {
                                Value = new TypedValue(10)
                            }
                        }
                    }
                }
            }
        };

        // Raise match initialization event.
        var eventManager = new EventManager();
        eventManager.HandleEvent(new MatchInitializationEvent(), new[] { matchInitializationTrigger }, entityManager);

        // Assert expected results.
        Assert.Equal(5, player1.Attributes["Deck"].AsEntityList().Count);
        Assert.Equal(3, player1.Attributes["Hand"].AsEntityList().Count);
        Assert.Equal(10, player1.Attributes["Health"].AsInt());

        Assert.Equal(5, player2.Attributes["Deck"].AsEntityList().Count);
        Assert.Equal(3, player2.Attributes["Hand"].AsEntityList().Count);
        Assert.Equal(10, player2.Attributes["Health"].AsInt());
    }
}
