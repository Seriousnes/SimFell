using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Gems;

public class Sapphire : Gem
{
    public override GemColor Color => GemColor.Sapphire;

    /// <summary>Blessing of the Prophet: seconds added to Spirit of Heroism.</summary>
    public double HeroismDurationBonus { get; private set; }

    protected override bool BindNode(Hero hero, GemNode node)
    {
        switch (node.Slot)
        {
            case 1: // Ancestral Surge: primary attribute bonus while Spirit of Heroism is active.
                var surge = hero.DefineAura(node.GrantedEffectId ?? throw new InvalidOperationException($"{node.Name} grants no effect"));
                hero.SpiritOfHeroismAuras.Add(surge);
                return true;
            case 2: // Mystic's Intuition: flat Stamina/Spirit rating (in the sheet ratings).
                return true;
            case 4: // Oracle's Foresight: Spirit.
                hero.Stats.BonusSpirit += node.Value("SpiritBonus");
                return true;
            case 5: // Blessing of the Prophet: longer Spirit of Heroism (the Spirit cost reduction does not affect damage).
                HeroismDurationBonus = node.Value("DurationBonus");
                hero.SpiritOfHeroismDurationBonus += HeroismDurationBonus;
                return true;
            default: // Resonating Soul is defensive.
                return false;
        }
    }
}
