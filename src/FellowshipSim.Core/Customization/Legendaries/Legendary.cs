using FellowshipSim.Core.Domain;

namespace FellowshipSim.Core.Customization.Legendaries;

public abstract class Legendary
{
    public abstract string Name { get; }

    /// <summary>False for legendaries the engine does not model yet (reported, no effect).</summary>
    public virtual bool IsModeled => true;

    public abstract void Bind(Hero hero);
    public virtual void Initialize(Codex.ICodexDataService codex) { }
}

