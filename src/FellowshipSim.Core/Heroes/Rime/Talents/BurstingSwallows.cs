using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>Talent 628: Frost Swallow hits have a 12% chance to trigger a Bursting Ice pulse on the target.</summary>
public class BurstingSwallows : Talent
{
    public const double ProcChance = 0.12;
    public override string Name => "Bursting Swallows";
    public override int Id => 628;
    public override void Bind(Hero hero) { }
}
