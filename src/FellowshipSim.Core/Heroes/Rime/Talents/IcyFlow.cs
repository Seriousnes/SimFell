using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>
/// Talent 44 / effect 2308 (15s, 2 stacks): Cold Snap grants a stack; the next Glacial Blast is cast with +50% Haste, the
/// next Ice Comet lands 0.5s sooner, and the one consuming it gets +20% crit chance (talent text and hero_data
/// "ReducesCastTimeOnOrbSpender" AddedCritChance 0.2; the buff's own text says +25%). Talon Strike and Rising Talons
/// consume it too (Icy Talons). UNVERIFIED: no combat log has this talent; Ice Comet's impact delay is not modeled.
/// </summary>
public class IcyFlow : Talent
{
    public const int EffectId = 2308;
    public const double CritChance = 0.2;
    public const double CastHaste = 0.5;
    public override string Name => "Icy Flow";
    public override int Id => 44;
    public override void Bind(Hero hero) { }
}
