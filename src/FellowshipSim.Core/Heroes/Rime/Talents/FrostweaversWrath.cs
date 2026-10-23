using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>
/// Talent 191 / effect 2305 (20s, 2 stacks): gaining Winter Orbs has a 20% chance to grant a stack; the next Glacial Blast
/// or Ice Comet (Talon Strike / Rising Talons) that deals damage has +100% crit chance (overflow is a grievous crit).
/// hero_data "ChanceOnOrbGain" (ProcChance 0.2, CritChance 1.0). Rolled once per gain that adds orbs, as
/// the old C++ sim did. UNVERIFIED: no combat log has this talent.
/// </summary>
public class FrostweaversWrath : Talent
{
    public const int TalentId = 191;
    public const string TalentName = "Frostweaver's Wrath";
    public const int EffectId = 2305;
    public const double BuffSeconds = 20.0;
    public const int MaxStacks = 2;
    public const double ProcChance = 0.2;
    public const double CritChance = 1.0;
    public const string ProcKey = "frostweavers_wrath";

    public static readonly AuraDef Buff = AuraDef.Effect("Frostweaver's Wrath", EffectId, BuffSeconds, MaxStacks);

    public override string Name => TalentName;
    public override int Id => TalentId;

    public override void Bind(Hero hero)
    {
        // "Each time you generate Winter Orbs" (one roll per orb gained).
        hero.OnResourceGained += (resource, _, _, ctx) =>
        {
            if (resource == Rime.WinterOrbsResource && ctx.Outcomes.RollProc(ProcKey, ProcChance, ctx)) hero.Auras.Apply(Buff, ctx.CurrentTime);
        };
        // "Your next Glacial Blast or Ice Comet" critically strikes: Talon Strike and Rising Talons count (they are built on
        // them), and so do the free comets of Cold Shower and Avalanche.
        hero.OnCastResolving += cast =>
        {
            if (cast.Ability is not (GlacialBlast or IceComet) || !hero.Auras.HasAura(Buff, cast.Time)) return;
            hero.Auras.ConsumeStack(Buff, cast.Time);
            cast.CritChance += CritChance;
        };
    }
}
