using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;

namespace FellowshipSim.Core.Customization.Gems;

public enum GemColor
{
    Ruby,
    Amethyst,
    Topaz,
    Emerald,
    Sapphire,
    Diamond
}

/// <summary>
/// A gem color's trait tree. Unlocked nodes (by <see cref="Power"/>) come from the datamine gem-trait records;
/// each color models the nodes it understands in <see cref="BindNode"/>.
/// </summary>
public abstract class Gem
{
    public abstract GemColor Color { get; }
    public double Power { get; set; }

    public IReadOnlyList<GemNode> Nodes { get; private set; } = Array.Empty<GemNode>();

    /// <summary>Unlocked nodes this color does not model yet.</summary>
    public List<string> UnmodeledNodes { get; } = new();

    public virtual void Initialize(ICodexDataService codex) { }

    public void Bind(Hero hero)
    {
        Nodes = RankData.UnlockedGemNodes(hero.Data, Color.ToString(), Power);
        foreach (var node in Nodes)
        {
            if (!BindNode(hero, node)) UnmodeledNodes.Add(node.Name);
        }
    }

    /// <summary>Applies one unlocked node to the hero. Returns false if the node is not modeled.</summary>
    protected abstract bool BindNode(Hero hero, GemNode node);

    public GemNode? NodeInSlot(int slot) => Nodes.FirstOrDefault(n => n.Slot == slot);
}
