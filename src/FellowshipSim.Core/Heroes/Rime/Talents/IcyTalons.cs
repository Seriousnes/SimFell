using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>Talent 658: Glacial Blast becomes Talon Strike and Ice Comet becomes Rising Talons.</summary>
public class IcyTalons : Talent
{
    public override string Name => "Icy Talons";
    public override int Id => 658;
    public override void Bind(Hero hero) { }
}
