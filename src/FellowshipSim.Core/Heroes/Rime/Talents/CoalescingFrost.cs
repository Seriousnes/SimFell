using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>
/// Talent 36: each Freezing Torrent hit adds a stack of Coalescing Frost (effect 2306) to its target for 3s (refreshed);
/// a crit has a 50% chance to add 2. When it expires, or the enemy dies (per the user), it erupts for 56% Intellect per
/// stack on enemies around it (damage effect 2300). Values: talent text; hero_data Freezing Torrent "DelayedAoe"
/// (DamageDividerToInt 0.56, TargetCountThreshold 5). UNVERIFIED: no combat log has this talent.
/// </summary>
public class CoalescingFrost : Talent
{
    public const int TalentId = 36;
    public const string TalentName = "Coalescing Frost";
    public const int DebuffEffectId = 2306;
    public const int DamageEffectId = 2300;
    public const double DurationSeconds = 3.0;
    public const double CoefficientPerStack = 0.56;
    public const double CritDoubleStackChance = 0.5;
    public const double TargetThreshold = 5;

    public override string Name => TalentName;
    public override int Id => TalentId;

    public override void Bind(Hero hero) => ((Rime)hero).GetAbility<CoalescingFrostEruption>()?.Enable(hero);
}
