namespace FellowshipSim.Core.Customization.Gems;

/// <summary>The Ruby gem tree. Node values are from the game data (gem-trait records).</summary>
public class Ruby : Gem
{
    public static readonly GemNode[] AllNodes =
    [
        new(296, 1, "Might of the Minotaur", 80, V(("PrimaryStatIncrease", 0.02), ("HealthThreshold", 0.8)),
            AuraDef.Effect("Might of the Minotaur", 1921, modifiers: StatModifier.More(StatType.PrimaryAttributeMultiplier, 1.02))),
        new(278, 2, "Champion's Heart", 150, V(("StaminaBonus", 12), ("PrimaryStatBonus", 2))),
        new(310, 3, "Unyielding Vitality", 220, V(("HealPercent", 0.007), ("Seconds", 2))),
        new(300, 4, "Titan's Blood", 300, V(("StaminaIncrease", 0.03))),
        new(330, 5, "Blessing of the Conqueror", 450, V(("DamageIncrease", 0.05)),
            AuraDef.Effect("Essence of the Conqueror", 1951, modifiers: StatModifier.More(StatType.DamageScale, 1.05))),
        new(297, 6, "Might of the Minotaur - II", 600, V(("PrimaryStatIncrease", 0.06), ("HealthThreshold", 0.8)),
            AuraDef.Effect("Might of the Minotaur - II", 1922, modifiers: StatModifier.More(StatType.PrimaryAttributeMultiplier, 1.06))),
        new(279, 7, "Champion's Heart - II", 800, V(("StaminaBonus", 36), ("PrimaryStatBonus", 6))),
        new(311, 8, "Unyielding Vitality - II", 1000, V(("HealPercent", 0.021), ("Seconds", 2))),
        new(301, 9, "Titan's Blood - II", 1250, V(("StaminaIncrease", 0.09))),
        new(331, 10, "Blessing of the Conqueror - II", 1500, V(("DamageIncrease", 0.15)),
            AuraDef.Effect("Essence of the Conqueror - II", 1952, modifiers: StatModifier.More(StatType.DamageScale, 1.15))),
    ];

    public override GemColor Color => GemColor.Ruby;
    protected override IReadOnlyList<GemNode> NodeTable => AllNodes;

    protected override bool BindNode(Hero hero, GemNode node)
    {
        switch (node.Slot)
        {
            case 1: // Might of the Minotaur: Main Stat while above 80% health (situational: the engine does not track your health).
            case 5: // Blessing of the Conqueror: damage while in combat with a boss (situational).
                hero.SituationalAuras.Add(node.Grants!);
                return true;
            case 2: // Champion's Heart: flat Main Stat (a logged Main Stat already includes it).
                if (!hero.Stats.UsesSheetValues) hero.Stats.PrimaryStat += node.Value("PrimaryStatBonus");
                return true;
            default: // Unyielding Vitality (healing) and Titan's Blood (Stamina) do not affect damage.
                return true;
        }
    }
}
