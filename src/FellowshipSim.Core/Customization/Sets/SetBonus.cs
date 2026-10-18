using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Sets;

public abstract class SetBonus
{
    public abstract string Name { get; }

    /// <summary>Parts of the bonus the engine does not model yet (reported).</summary>
    public virtual IReadOnlyList<string> UnmodeledParts => Array.Empty<string>();

    public abstract void Bind(Hero hero);
}
