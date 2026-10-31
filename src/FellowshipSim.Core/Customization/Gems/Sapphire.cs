namespace FellowshipSim.Core.Customization.Gems;

public class Sapphire : Gem
{
    public const int AncestralSurgeEffectId = 1919;
    public const int AncestralSurgeIIEffectId = 1920;

    public static readonly GemNode[] AllNodes =
    [
        new(294, 1, "Ancestral Surge", 80, NodeValues(new NodeValue("SpiritPointsBonus", 10), new NodeValue("PrimaryStatBonus", 0.08)),
            AuraDef.Effect("Ancestral Surge", AncestralSurgeEffectId, modifiers: StatModifier.More(StatType.PrimaryAttributeMultiplier, 1.08))),
        new(276, 2, "Mystic's Intuition", 150, NodeValues(new NodeValue("StaminaBonus", 4), new NodeValue("SpiritBonus", 8))),
        new(304, 3, "Resonating Soul", 220, NodeValues(new NodeValue("HealthInterval", 0.1), new NodeValue("DamageReduction", 0.01))),
        new(286, 4, "Oracle's Foresight", 300, NodeValues(new NodeValue("SpiritBonus", 0.02))),
        new(308, 5, "Blessing of the Prophet", 450, NodeValues(new NodeValue("CostReduction", 0.05), new NodeValue("DurationBonus", 6))),
        new(295, 6, "Ancestral Surge - II", 600, NodeValues(new NodeValue("SpiritPointsBonus", 30), new NodeValue("PrimaryStatBonus", 0.24)),
            AuraDef.Effect("Ancestral Surge - II", AncestralSurgeIIEffectId, modifiers: StatModifier.More(StatType.PrimaryAttributeMultiplier, 1.24))),
        new(277, 7, "Mystic's Intuition - II", 800, NodeValues(new NodeValue("StaminaBonus", 12), new NodeValue("SpiritBonus", 24))),
        new(305, 8, "Resonating Soul - II", 1000, NodeValues(new NodeValue("HealthInterval", 0.1), new NodeValue("DamageReduction", 0.03))),
        new(287, 9, "Oracle's Foresight - II", 1250, NodeValues(new NodeValue("SpiritBonus", 0.06))),
        new(309, 10, "Blessing of the Prophet - II", 1500, NodeValues(new NodeValue("CostReduction", 0.15), new NodeValue("DurationBonus", 18))),
    ];

    public override GemColor Color => GemColor.Sapphire;
    protected override IReadOnlyList<GemNode> NodeTable => AllNodes;

    /// <summary>Blessing of the Prophet: seconds added to Spirit of Heroism.</summary>
    public double HeroismDurationBonus { get; private set; }

    protected override bool BindNode(Hero hero, GemNode node)
    {
        switch (node.Slot)
        {
            case HeadlineSlot: // Ancestral Surge: more maximum Spirit Points, and Main Stat while Spirit of Heroism is active.
                hero.AddMaxSpiritPoints(node.Value(SpiritPointsBonusKey));
                hero.SpiritOfHeroismAuras.Add(hero.Register(node.Grants!));
                return true;
            case FlatStatSlot: // Mystic's Intuition: flat Stamina and Spirit rating.
                hero.Stats.SpiritRating += node.Value(SpiritBonusKey);
                hero.Stats.Stamina += node.Value(StaminaBonusKey);
                return true;
            case PercentStatSlot: // Oracle's Foresight: Spirit.
                hero.Stats.BonusSpirit += node.Value(SpiritBonusKey);
                return true;
            case BlessingSlot: // Blessing of the Prophet: a cheaper Spirit ability and a longer Spirit of Heroism.
                hero.SpiritAbilityCostReduction += node.Value(CostReductionKey);
                HeroismDurationBonus = node.Value(DurationBonusKey);
                hero.SpiritOfHeroismDurationBonus += HeroismDurationBonus;
                return true;
            default: // Resonating Soul (damage reduction) does not affect damage.
                return true;
        }
    }

    protected override void BindLater(Hero hero, GemNode node, SimTime now)
    {
        if (node.Slot != HeadlineSlot) return;
        hero.AddMaxSpiritPoints(node.Value(SpiritPointsBonusKey));
        hero.SpiritOfHeroismAuras.Add(hero.Register(node.Grants!));
    }

    /// <summary>Spirit of Heroism gives the active rank's Ancestral Surge, and the maximum Spirit Points follow the rank.</summary>
    protected override void NodesChanged(Hero hero, IReadOnlyList<GemNode> before, SimTime? now)
    {
        var old = before.FirstOrDefault(n => n.Slot == HeadlineSlot);
        var next = NodeInSlot(HeadlineSlot);
        if (old == null || next == null || old == next) return;
        var index = hero.SpiritOfHeroismAuras.IndexOf(old.Grants!);
        if (index >= 0) hero.SpiritOfHeroismAuras[index] = hero.Register(next.Grants!);
        hero.AddMaxSpiritPoints(next.Value(SpiritPointsBonusKey) - old.Value(SpiritPointsBonusKey));
    }

    protected override IEnumerable<StatModifier> StatEffects(GemNode node) => node.Slot switch
    {
        FlatStatSlot => [StatModifier.Add(StatType.SpiritRating, node.Value(SpiritBonusKey))],
        PercentStatSlot => [StatModifier.Add(StatType.Spirit, node.Value(SpiritBonusKey))],
        _ => [],
    };
}
