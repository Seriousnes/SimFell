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
/// One node of a gem colour's tree. Nodes 1-5 are the rank I versions of the five slots; nodes 6-10 are rank II and
/// replace the rank I node of the same slot once unlocked. A node unlocks at <see cref="RequiredPower"/> gem power.
/// </summary>
public sealed record GemNode(int Id, int Node, string Name, double RequiredPower, IReadOnlyDictionary<string, double> Values, AuraDef? Grants = null)
{
    /// <summary>The node's slot in its row (1-5), shared by its rank I and rank II versions.</summary>
    public int Slot => (Node - 1) % Gem.SlotsPerRank + 1;
    public bool IsRankTwo => Node > Gem.SlotsPerRank;
    public double Value(string key) => Values.TryGetValue(key, out var value) ? value : throw new KeyNotFoundException($"Gem node {Name} has no value {key}");
}

/// <summary>
/// A gem colour's tree. Each colour lists its nodes (with their game values) at the top of its class, and applies
/// the unlocked ones it models in <see cref="BindNode"/>.
/// </summary>
public abstract class Gem
{
    /// <summary>
    /// A colour's active nodes are the highest five whose required power is at or below the gem power (UCRGemSubsystem
    /// 0x145388B10, build 26162532), so a rank II node replaces the rank I node of its slot.
    /// </summary>
    public const int ActiveGemNodes = 5;

    /// <summary>
    /// Gem power over this counts toward the overcap bonus (GemBonus.GlobalGemPowerCapValue, read by
    /// GEC_Shared_GemOvercapBonus_OvercapCalculation, effect 2469): Main Stat and Stamina Multiplicitive 1 +
    /// <see cref="OvercapBonusPerPower"/> per point over it, summed over the colours.
    /// </summary>
    public const double OvercapPower = 1500;
    public const double OvercapBonusPerPower = 0.00005;

    /// <summary>Slots in one rank of a colour's tree: nodes 1-5 are rank I and nodes 6-10 rank II (see <see cref="GemNode"/>).</summary>
    public const int SlotsPerRank = 5;

    /// <summary>Slot of a colour's headline node (Sealed Fate, Harmonious Soul, First Strike and the like).</summary>
    public const int HeadlineSlot = 1;

    /// <summary>Slot of a colour's node with flat Stamina and a flat stat (Berserker's Zeal, Stoic's Teachings and the like).</summary>
    public const int FlatStatSlot = 2;

    /// <summary>Slot of a colour's node with a percent stat (Killer Instinct, Ancient's Wisdom and the like).</summary>
    public const int PercentStatSlot = 4;

    /// <summary>Slot of a colour's blessing node (Blessing of the Deathdealer and the like).</summary>
    public const int BlessingSlot = 5;

    // Keys of the node values the colours read. Each is written beside its value in the colours' node tables.
    protected const string CooldownReductionKey = "CooldownReduction";
    protected const string CostReductionKey = "CostReduction";
    protected const string CritBonusKey = "CritBonus";
    protected const string CritChanceBonusKey = "CritChanceBonus";
    protected const string CritPowerBonusKey = "CritPowerBonus";
    protected const string DurationBonusKey = "DurationBonus";
    protected const string ExpertiseBonusKey = "ExpertiseBonus";
    protected const string HasteBonusKey = "HasteBonus";
    protected const string HealthThresholdKey = "HealthThreshold";
    protected const string PrimaryStatBonusKey = "PrimaryStatBonus";
    protected const string SpiritBonusKey = "SpiritBonus";
    protected const string SpiritPointsBonusKey = "SpiritPointsBonus";
    protected const string StaminaBonusKey = "StaminaBonus";
    protected const string StaminaIncreaseKey = "StaminaIncrease";

    public abstract GemColor Color { get; }
    public double Power { get; set; }

    /// <summary>The tree of gem colour <paramref name="color"/>.</summary>
    public static Gem Create(GemColor color) => color switch
    {
        GemColor.Emerald => new Emerald(),
        GemColor.Ruby => new Ruby(),
        GemColor.Amethyst => new Amethyst(),
        GemColor.Topaz => new Topaz(),
        GemColor.Sapphire => new Sapphire(),
        _ => new Diamond(),
    };

    /// <summary>The power the gem was bound with; <see cref="Power"/> moves from it while a live change lasts.</summary>
    public double BasePower { get; private set; }

    /// <summary>Every node of this colour's tree.</summary>
    protected abstract IReadOnlyList<GemNode> NodeTable { get; }

    /// <summary>Every node of this colour's tree, as <see cref="NodeTable"/>.</summary>
    public IReadOnlyList<GemNode> Tree => NodeTable;

    /// <summary>The nodes active at <see cref="Power"/>.</summary>
    public IReadOnlyList<GemNode> Nodes { get; private set; } = Array.Empty<GemNode>();

    /// <summary>Unlocked nodes this colour does not model yet.</summary>
    public List<string> UnmodeledNodes { get; } = new();

    private Hero? _hero;
    private IReadOnlyList<GemNode> _baseNodes = Array.Empty<GemNode>();
    private readonly HashSet<int> _boundSlots = new();
    private AuraDef? _liveStats;

    public void Bind(Hero hero)
    {
        _hero = hero;
        BasePower = Power;
        Nodes = _baseNodes = ActiveNodesAt(Power);
        foreach (var node in Nodes)
        {
            _boundSlots.Add(node.Slot);
            if (!BindNode(hero, node)) UnmodeledNodes.Add(node.Name);
            else if (node.Grants != null) hero.Register(node.Grants);
        }
    }

    /// <summary>The nodes active at <paramref name="power"/>, in slot order.</summary>
    public IReadOnlyList<GemNode> ActiveNodesAt(double power)
    {
        var whole = (int)power;
        var active = new List<GemNode>();
        for (var i = NodeTable.Count - 1; i >= 0 && active.Count < ActiveGemNodes; i--)
        {
            if (whole >= NodeTable[i].RequiredPower) active.Add(NodeTable[i]);
        }
        return active.OrderBy(n => n.Slot).ToList();
    }

    /// <summary>
    /// The gem power changes while the hero fights (Anomalous Residue): the nodes re-evaluate at once, the lower node of a
    /// slot coming back when the power falls (UCRGemPowerComponent's power callback 0x145390CE0, build 26162532). A node's
    /// stats change by their difference from the bound nodes'; an aura a node grants passes to the slot's new node.
    /// </summary>
    public void ChangePower(double power, SimTime now)
    {
        var hero = _hero ?? throw new InvalidOperationException("The gem is not bound.");
        var before = Nodes;
        Power = power;
        var after = ActiveNodesAt(power);
        if (!after.SequenceEqual(before))
        {
            Nodes = after;
            foreach (var node in after)
            {
                if (node.Grants != null) hero.Register(node.Grants);
                if (_boundSlots.Add(node.Slot)) BindLater(hero, node, now);
            }
            for (var slot = 1; slot <= SlotsPerRank; slot++)
            {
                var old = before.FirstOrDefault(n => n.Slot == slot);
                var next = after.FirstOrDefault(n => n.Slot == slot);
                if (old != next) SwapGrant(hero, old, next, now);
            }
            NodesChanged(hero, before, now);
        }
        else
        {
            after = before;
        }

        // The stats follow every power change: the overcap moves with the power whether the nodes change or not.
        var modifiers = new List<StatModifier>();
        foreach (var node in after.Except(_baseNodes)) modifiers.AddRange(StatEffects(node));
        foreach (var node in _baseNodes.Except(after)) modifiers.AddRange(StatEffects(node).Select(Inverse));
        // The overcap bonus captures the live power (no snapshot); the bound power's is in the bound stats.
        var overcap = Math.Max(0.0, power - OvercapPower) - Math.Max(0.0, BasePower - OvercapPower);
        if (overcap != 0.0) modifiers.Add(StatModifier.Increased(StatType.PrimaryAttributeMultiplier, 1.0 + OvercapBonusPerPower * overcap));
        if (_liveStats != null) hero.Auras.Remove(_liveStats, now);
        _liveStats = modifiers.Count == 0 ? null : AuraDef.Internal($"{Color} gem power", 0, 1, modifiers.ToArray());
        if (_liveStats != null) hero.Auras.Apply(_liveStats, now);
    }

    /// <summary>A fight starts with the bound power and nodes.</summary>
    public void ResetLive()
    {
        if (_hero == null || (Nodes == _baseNodes && _liveStats == null && Power == BasePower)) return;
        var before = Nodes;
        Power = BasePower;
        Nodes = _baseNodes;
        _liveStats = null;
        if (before != _baseNodes) NodesChanged(_hero, before, null);
    }

    private static StatModifier Inverse(StatModifier modifier) => modifier.Op switch
    {
        ModifierOp.Additive => modifier with { Value = -modifier.Value },
        ModifierOp.MultiplyAdditive => modifier with { Value = 2.0 - modifier.Value },
        _ => modifier with { Value = 1.0 / modifier.Value },
    };

    /// <summary>An aura the old node of a slot granted passes, with its stacks, to the new node's.</summary>
    private static void SwapGrant(Hero hero, GemNode? old, GemNode? next, SimTime now)
    {
        if (old?.Grants is not { } oldAura || hero.Auras.Get(oldAura, now) is not { } active) return;
        var stacks = active.Stacks;
        hero.Auras.Remove(oldAura, now);
        if (next?.Grants is { } nextAura) hero.Auras.Apply(nextAura, now, setStacks: Math.Min(stacks, nextAura.MaxStacks));
    }

    /// <summary>Applies one unlocked node to the hero. Returns false if the node is not modeled.</summary>
    protected abstract bool BindNode(Hero hero, GemNode node);

    /// <summary>
    /// Binds the effects that follow the slot's active node (see <see cref="NodeInSlot"/>) for a slot a live power change
    /// reaches first; its stats come through <see cref="StatEffects"/>.
    /// </summary>
    protected virtual void BindLater(Hero hero, GemNode node, SimTime now) { }

    /// <summary>The stats a node changes, for live power changes (its bound effects stay as they are).</summary>
    protected virtual IEnumerable<StatModifier> StatEffects(GemNode node) => [];

    /// <summary>
    /// The active nodes changed from <paramref name="before"/>: at <paramref name="now"/> in a fight, or back to the bound
    /// ones as a fight starts (null).
    /// </summary>
    protected virtual void NodesChanged(Hero hero, IReadOnlyList<GemNode> before, SimTime? now) { }

    public GemNode? NodeInSlot(int slot) => Nodes.FirstOrDefault(n => n.Slot == slot);

    protected static Dictionary<string, double> NodeValues(params NodeValue[] values) => values.ToDictionary(v => v.Key, v => v.Value);
}
