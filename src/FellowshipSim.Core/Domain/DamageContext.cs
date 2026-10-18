using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Domain;

public record DamageContext(
    Hero Source,
    EnemyTarget? Target,
    int AbilityId,
    string AbilityName,
    double Amount,
    bool IsCrit,
    SimTime Timestamp,
    SimulationContext? SimContext = null
);
