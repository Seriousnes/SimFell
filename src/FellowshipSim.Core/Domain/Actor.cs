namespace FellowshipSim.Core.Domain;

public abstract class Actor
{
    public int Id { get; init; }
    public int InstanceId { get; init; } = 1;
    public string Name { get; set; } = string.Empty;
    public bool IsSleeping
    {
        get => _isSleeping;
        internal set
        {
            if (_isSleeping == value) return;
            _isSleeping = value;
            SleepChanged?.Invoke();
        }
    }
    private bool _isSleeping;

    /// <summary>The simulation's list of active enemies is rebuilt when this fires.</summary>
    internal Action? SleepChanged;

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

