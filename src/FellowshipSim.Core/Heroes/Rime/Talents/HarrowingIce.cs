using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>
/// Talent 627 / effect 2839 (12s, 31 stacks): each Bursting Ice pulse grants a stack; Bursting Ice deals 3% more damage per
/// stack; the stack after 30 consumes them all and takes 5s off Flight of the Navir's cooldown.
/// Values: talent text; hero_data Bursting Ice "StackingDamageBonus" (DamageScalerPerStack 0.03, MaxStacks 31, 5s).
/// Combat logs (g6zBdpvJCmh2HfrD fight 31, 1056 pulses): a pulse deals damage with the stacks held before it, then adds
/// its stack; stacks reach 31 and are removed at once.
/// </summary>
public class HarrowingIce : Talent
{
    public const int TalentId = 627;
    public const string TalentName = "Harrowing Ice";
    public const int EffectId = 2839;
    public const double BuffSeconds = 12.0;
    public const int MaxStacks = 31;
    public const double DamagePerStack = 0.03;
    public const double FlightOfTheNavirReductionSeconds = 5.0;

    public static readonly AuraDef Buff = AuraDef.Effect("Harrowing Ice", EffectId, BuffSeconds, MaxStacks);
    public override string Name => TalentName;
    public override int Id => TalentId;
    public override void Bind(Hero hero) { }
}
