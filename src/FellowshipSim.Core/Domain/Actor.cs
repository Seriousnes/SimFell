using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Domain;

public abstract class Actor
{
    public int Id { get; init; }
    public int InstanceId { get; init; } = 1;
    public string Name { get; init; } = string.Empty;
    public bool IsSleeping { get; set; } = false;

    public ResourcePool Resources { get; } = new();
    public AuraContainer Auras { get; } = new();
    public StatBlock Stats { get; set; } = new();

    public virtual void Arise(SimulationContext ctx) => IsSleeping = false;

    public virtual void Demise(SimulationContext ctx)
    {
        IsSleeping = true;
        Auras.ClearAll();
    }
}

