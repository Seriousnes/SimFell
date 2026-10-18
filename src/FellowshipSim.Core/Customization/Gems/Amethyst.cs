using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Gems;

public class Amethyst : Gem
{
    public override GemColor Color => GemColor.Amethyst;

    protected override bool BindNode(Hero hero, GemNode node)
    {
        switch (node.Slot)
        {
            case 1: // Sealed Fate: extra crit chance against characters above a health threshold.
                double threshold = node.Value("HealthThreshold");
                double bonus = node.Value("CritChanceBonus");
                hero.TargetCritChanceBonuses.Add((target, _) => target.HealthFraction > threshold ? bonus : 0.0);
                return true;
            case 2: // Berserker's Zeal: flat Stamina/Crit rating (in the sheet ratings).
                return true;
            case 4: // Killer Instinct: crit chance.
                hero.Stats.BonusCritChance += node.Value("CritChanceBonus");
                return true;
            case 5: // Blessing of the Deathdealer: critical strikes do more damage.
                hero.PassiveAuras.Add(AuraDef.Internal(node.Name, 0, 1,
                    StatModifier.More(StatType.CritMultiplier, 1.0 + node.Value("CritPowerBonus"))));
                return true;
            default: // Reaper's Reprieve (healing) is defensive.
                return false;
        }
    }
}
