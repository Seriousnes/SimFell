namespace FellowshipSim.Core.Customization.Gems;

/// <summary>The Diamond gem tree. Node values are from the game data (gem-trait records).</summary>
public class Diamond : Gem
{
    public const double HarmoniousSoulSeconds = 5.0;
    public const double HarmoniousSoulStatPerStack = 0.006;

    public static readonly GemNode[] AllNodes =
    [
        new(338, 1, "Harmonious Soul", 80, V(("StatBonus", HarmoniousSoulStatPerStack), ("Duration", HarmoniousSoulSeconds), ("MaxStacks", 4), ("StackLoss", 1))),
        new(298, 2, "Stoic's Teachings", 150, V(("ArmorBonus", 40), ("PrimaryStatBonus", 4))),
        new(302, 3, "Tranquil Resolve", 220, V(("DamageReduction", 0.08), ("Duration", 6), ("Cooldown", 20))),
        new(292, 4, "Ancient's Wisdom", 300, V(("PrimaryStatBonus", 1.02)),
            AuraDef.Effect("Ancient's Wisdom", 1917, modifiers: StatModifier.More(StatType.PrimaryAttributeMultiplier, 1.02))),
        new(290, 5, "Blessing of the Artisan", 450, V(("CooldownReduction", 0.08))),
        new(339, 6, "Harmonious Soul - II", 600, V(("StatBonus", HarmoniousSoulStatPerStack), ("Duration", HarmoniousSoulSeconds), ("MaxStacks", 12), ("StackLoss", 1))),
        new(299, 7, "Stoic's Teachings - II", 800, V(("ArmorBonus", 120), ("PrimaryStatBonus", 12))),
        new(303, 8, "Tranquil Resolve - II", 1000, V(("DamageReduction", 0.24), ("Duration", 6), ("Cooldown", 20))),
        new(293, 9, "Ancient's Wisdom - II", 1250, V(("PrimaryStatBonus", 1.06)),
            AuraDef.Effect("Ancient's Wisdom - II", 1918, modifiers: StatModifier.More(StatType.PrimaryAttributeMultiplier, 1.06))),
        new(291, 10, "Blessing of the Artisan - II", 1500, V(("CooldownReduction", 0.24))),
    ];

    public override GemColor Color => GemColor.Diamond;
    protected override IReadOnlyList<GemNode> NodeTable => AllNodes;

    /// <summary>The Harmonious Soul buff of the unlocked rank, if modeled (Diamond Strike reads its stacks).</summary>
    public AuraDef? HarmoniousSoul { get; private set; }

    protected override bool BindNode(Hero hero, GemNode node)
    {
        switch (node.Slot)
        {
            case 1: // Harmonious Soul: "Each time an enemy you are in combat with is defeated, you gain +0.6% Critical Strike,
                    // Haste, Expertise and Spirit for 5 seconds, stacking up to 4 (II: 12) times." Not modeled: no combat log shows
                    // it (yLzJ8M391AwYfVTK fight 32: three players with Diamond power, enemies dying all fight, no Harmonious
                    // Soul buff), and the game data names no effect for it. Diamond Strike reads no stacks until it is.
                return false;
            case 2: // Stoic's Teachings: flat Main Stat (a logged Main Stat already includes it) and Armor.
                if (!hero.Stats.UsesSheetValues) hero.Stats.PrimaryStat += node.Value("PrimaryStatBonus");
                return true;
            case 4: // Ancient's Wisdom: Main Stat (a logged Main Stat already includes it).
                if (!hero.Stats.UsesSheetValues) hero.PassiveAuras.Add(node.Grants!);
                return true;
            default: // Tranquil Resolve (defensive) and Blessing of the Artisan (Relic cooldowns) do not affect damage.
                return true;
        }
    }
}
