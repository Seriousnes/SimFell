namespace FellowshipSim.Core.Customization.Gems;

/// <summary>Not modeled yet: every unlocked node is reported. Node values are in the game data (gem-trait records).</summary>
public class Topaz : Gem
{
    public static readonly GemNode[] AllNodes =
    [
        new(318, 1, "Adrenaline Rush", 80, V()),
        new(280, 2, "Thief's Alacrity", 150, V()),
        new(344, 3, "Rogue's Resurgence", 220, V()),
        new(320, 4, "Feline Grace", 300, V()),
        new(316, 5, "Blessing of the Virtuoso", 450, V()),
        new(319, 6, "Adrenaline Rush - II", 600, V()),
        new(281, 7, "Thief's Alacrity - II", 800, V()),
        new(345, 8, "Rogue's Resurgence - II", 1000, V()),
        new(321, 9, "Feline Grace - II", 1250, V()),
        new(317, 10, "Blessing of the Virtuoso - II", 1500, V()),
    ];

    public override GemColor Color => GemColor.Topaz;
    protected override IReadOnlyList<GemNode> NodeTable => AllNodes;
    protected override bool BindNode(Hero hero, GemNode node) => false;
}
