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
    public const int TalentId = 44;
    public const string TalentName = "Icy Flow";
    public const int EffectId = 2308;
    public const double BuffSeconds = 15.0;
    public const int MaxStacks = 2;
    public const double CritChance = 0.2;
    public const double CastHaste = 0.5;
    /// <summary>"causing your next Ice Comet to have 0.5 seconds reduced impact delay" (hero_data InitialDelayReductionInSeconds).</summary>
    public const double ImpactDelayReductionSeconds = 0.5;

    public static readonly AuraDef Buff = AuraDef.Effect("Icy Flow", EffectId, BuffSeconds, MaxStacks);

    public override string Name => TalentName;
    public override int Id => TalentId;

    /// <summary>
    /// Cold Snap grants a stack; the next spender uses it up for crit, a Glacial Blast cast with it is faster and an Ice
    /// Comet cast with it falls faster.
    /// </summary>
    public override void Bind(Hero hero)
    {
        hero.OnCastCompleted += (ability, _, now) =>
        {
            if (ability is ColdSnap) hero.Auras.Apply(Buff, now);
        };
        // The next Glacial Blast or Ice Comet (Talon Strike and Rising Talons count; Cold Shower's comet too, but not
        // Avalanche's extra comets).
        hero.OnCastResolving += cast =>
        {
            if (cast.Ability is not (GlacialBlast or IceComet) || cast.Source is Avalanche || !hero.Auras.HasAura(Buff, cast.Time)) return;
            hero.Auras.ConsumeStack(Buff, cast.Time);
            cast.CritChance += CritChance;
            if (cast.Ability is IceComet) cast.DelayReductionSeconds += ImpactDelayReductionSeconds;
        };
        // "+50% Haste to you while casting" the Glacial Blast that consumes it.
        hero.GetAbility<GlacialBlast>()?.CastHasteBonuses.Add(now => hero.Auras.HasAura(Buff, now) ? CastHaste : 0.0);
    }
}
