namespace FellowshipSim.Core.Customization.Gems;

public class Sapphire : Gem
{
    public const int AncestralSurgeEffectId = 1919;
    public const int AncestralSurgeIIEffectId = 1920;

    public static readonly GemNode[] AllNodes =
    [
        new(294, 1, "Ancestral Surge", 80, V(("SpiritPointsBonus", 10), ("PrimaryStatBonus", 0.08)),
            AuraDef.Effect("Ancestral Surge", AncestralSurgeEffectId, modifiers: StatModifier.More(StatType.PrimaryAttributeMultiplier, 1.08))),
        new(276, 2, "Mystic's Intuition", 150, V(("StaminaBonus", 8), ("SpiritBonus", 8))),
        new(304, 3, "Resonating Soul", 220, V(("HealthInterval", 0.1), ("DamageReduction", 0.01))),
        new(286, 4, "Oracle's Foresight", 300, V(("SpiritBonus", 0.02))),
        new(308, 5, "Blessing of the Prophet", 450, V(("CostReduction", 0.05), ("DurationBonus", 6))),
        new(295, 6, "Ancestral Surge - II", 600, V(("SpiritPointsBonus", 30), ("PrimaryStatBonus", 0.24)),
            AuraDef.Effect("Ancestral Surge - II", AncestralSurgeIIEffectId, modifiers: StatModifier.More(StatType.PrimaryAttributeMultiplier, 1.24))),
        new(277, 7, "Mystic's Intuition - II", 800, V(("StaminaBonus", 24), ("SpiritBonus", 24))),
        new(305, 8, "Resonating Soul - II", 1000, V(("HealthInterval", 0.1), ("DamageReduction", 0.03))),
        new(287, 9, "Oracle's Foresight - II", 1250, V(("SpiritBonus", 0.06))),
        new(309, 10, "Blessing of the Prophet - II", 1500, V(("CostReduction", 0.15), ("DurationBonus", 18))),
    ];

    public override GemColor Color => GemColor.Sapphire;
    protected override IReadOnlyList<GemNode> NodeTable => AllNodes;

    /// <summary>Blessing of the Prophet: seconds added to Spirit of Heroism.</summary>
    public double HeroismDurationBonus { get; private set; }

    protected override bool BindNode(Hero hero, GemNode node)
    {
        switch (node.Slot)
        {
            case 1: // Ancestral Surge: primary attribute bonus while Spirit of Heroism is active.
                hero.SpiritOfHeroismAuras.Add(hero.Register(node.Grants!));
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
            default: // Resonating Soul (damage reduction) does not affect damage.
                return true;
        }
    }
}
