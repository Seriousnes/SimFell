using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

// Values come from the datamine records named in each class (ability IDs unless noted as effects).

/// <summary>Base for Rime's abilities, built from a datamine ability record.</summary>
public abstract class RimeAbility : Ability
{
    protected RimeAbility(Rime owner, int abilityId) : base(owner, abilityId) { }
    protected RimeAbility(Rime owner, int id, string name, double coefficient) : base(owner, id, name, coefficient) { }

    protected Rime Rime => (Rime)Owner;
}
