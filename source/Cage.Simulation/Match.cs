using System.Linq;
using Cage.Simulation.Entities;

namespace Cage.Simulation;

public class Match
{
    private readonly EntityManager entityManager;

    public EntityManager EntityManager => entityManager;

    public Match() : this(new EntityManager())
    {
    }

    public Match(EntityManager entityManager)
    {
        this.entityManager = entityManager;
    }
}