namespace FellowshipSim.Core.Customization.Gems;

/// <summary>Not modeled yet: every unlocked node is reported. Node values are in the game data (gem-trait records).</summary>
public class Ruby : Gem
{
    public static readonly GemNode[] AllNodes =
    [
        new(296, 1, "Might of the Minotaur", 80, V()),
        new(278, 2, "Champion's Heart", 150, V()),
        new(310, 3, "Unyielding Vitality", 220, V()),
        new(300, 4, "Titan's Blood", 300, V()),
        new(330, 5, "Blessing of the Conqueror", 450, V()),
        new(297, 6, "Might of the Minotaur - II", 600, V()),
        new(279, 7, "Champion's Heart - II", 800, V()),
        new(311, 8, "Unyielding Vitality - II", 1000, V()),
        new(301, 9, "Titan's Blood - II", 1250, V()),
        new(331, 10, "Blessing of the Conqueror - II", 1500, V()),
    ];

    public override GemColor Color => GemColor.Ruby;
    protected override IReadOnlyList<GemNode> NodeTable => AllNodes;
    protected override bool BindNode(Hero hero, GemNode node) => false;
}
