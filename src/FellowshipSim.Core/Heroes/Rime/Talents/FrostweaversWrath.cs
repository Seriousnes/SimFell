using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>
/// Talent 191 / effect 2305 (12s, 2 stacks): gaining Winter Orbs has a 20% chance to grant a stack; the next Glacial Blast
/// or Ice Comet (Talon Strike / Rising Talons) that deals damage has +100% crit chance (overflow is a grievous crit).
/// hero_data "ChanceOnOrbGain" (ProcChance 0.2, CritChance 1.0, Duration 12). Rolled once per gain that adds orbs, as
/// the old C++ sim did. UNVERIFIED: no combat log has this talent.
/// </summary>
public class FrostweaversWrath : Talent
{
    public const int TalentId = 191;
    public const string TalentName = "Frostweaver's Wrath";
    public const int EffectId = 2305;
    public const double BuffSeconds = 12.0;
    public const int MaxStacks = 2;
    public const double ProcChance = 0.2;
    public const double CritChance = 1.0;
    public const string ProcKey = "frostweavers_wrath";

    public static readonly AuraDef Buff = AuraDef.Effect("Frostweaver's Wrath", EffectId, BuffSeconds, MaxStacks);

    public override string Name => TalentName;
    public override int Id => TalentId;

    public override void Bind(Hero hero)
    {
        hero.Register(Buff);
        // "Each time you generate Winter Orbs" (one roll per gain that adds orbs).
        ((Rime)hero).WinterOrbsGained += (ctx, _) =>
        {
            if (ctx.Outcomes.RollProc(ProcKey, ProcChance, ctx, Buff.LogId)) hero.Auras.Apply(Buff, ctx.CurrentTime);
        };
    }
}
