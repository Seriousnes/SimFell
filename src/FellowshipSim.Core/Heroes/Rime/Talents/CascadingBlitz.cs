using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>Talent 192: each Anima generated during Ice Blitz conjures 1 Frost Swallow.</summary>
public class CascadingBlitz : Talent
{
    public override string Name => "Cascading Blitz";
    public override int Id => 192;
    public override void Bind(Hero hero) { }
}
