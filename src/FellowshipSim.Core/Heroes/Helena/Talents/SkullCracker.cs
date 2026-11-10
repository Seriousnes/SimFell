using FellowshipSim.Core.Heroes.Helena.Abilities;

namespace FellowshipSim.Core.Heroes.Helena.Talents;

/// <summary>
/// Talent 214: "Your Shield Throw has +20% additional critical strike chance and deals 100% increased damage to the first
/// target it hits."
/// </summary>
public partial class SkullCracker : Talent
{
    public override string Name => TalentName;
    public override int Id => TalentId;

    public override void Bind(Hero hero)
    {
        if (hero.GetAbility<ShieldThrow>() is not { } shieldThrow) return;
        shieldThrow.CritChanceBonus += INCREASED_CRIT_CHANCE_ADDITIONAL_CRIT_CHANCE;
        shieldThrow.FirstHitMultipliers.Add(_ => FIRST_HIT_DAMAGE_DAMAGE_INCREASE);
    }
}
