namespace FellowshipSim.Core.Customization.Gems;

/// <summary>Not modeled yet: every unlocked node is reported. Node values are in the game data (gem-trait records).</summary>
public class Diamond : Gem
{
    public static readonly GemNode[] AllNodes =
    [
        new(338, 1, "Harmonious Soul", 80, V()),
        new(298, 2, "Stoic's Teachings", 150, V()),
        new(302, 3, "Tranquil Resolve", 220, V()),
        new(292, 4, "Ancient's Wisdom", 300, V()),
        new(290, 5, "Blessing of the Artisan", 450, V()),
        new(339, 6, "Harmonious Soul - II", 600, V()),
        new(299, 7, "Stoic's Teachings - II", 800, V()),
        new(303, 8, "Tranquil Resolve - II", 1000, V()),
        new(293, 9, "Ancient's Wisdom - II", 1250, V()),
        new(291, 10, "Blessing of the Artisan - II", 1500, V()),
    ];

    public override GemColor Color => GemColor.Diamond;
    protected override IReadOnlyList<GemNode> NodeTable => AllNodes;
    protected override bool BindNode(Hero hero, GemNode node) => false;
}
