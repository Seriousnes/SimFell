using FellowshipSim.Core.Customization.Gems;

namespace FellowshipSim.Core.Import;

/// <summary>A trait or blessing in a build: its game ID, its rank, and the name the log gave it (for reports).</summary>
public sealed record BuildEntry(int Id, int Rank = 1, string? Name = null);

/// <summary>
/// A hero's build by game IDs: character-sheet stats, talents, traits, blessings, gem powers, set pieces, legendary items
/// and weapon. FellowshipSim.Logs builds it from a combat log; it can also be written or edited by hand.
/// <see cref="HeroImporter"/> turns it into a hero ready to simulate.
/// </summary>
public class CharacterBuild
{
    public string Name { get; set; } = "";

    /// <summary>The hero's name, e.g. "Rime" (its class in Heroes/).</summary>
    public string Hero { get; set; } = "";

    // ---- character sheet
    public double PrimaryStat { get; set; }
    public double Stamina { get; set; }
    public double Armor { get; set; }
    public double CritRating { get; set; }
    public double HasteRating { get; set; }
    public double ExpertiseRating { get; set; }
    public double SpiritRating { get; set; }

    /// <summary>
    /// Character-sheet percentages ("crit", "haste", "expertise", "spirit") as logged. When set, they are used as the
    /// hero's base percentages (they already include passive bonuses such as gem nodes and set bonuses).
    /// </summary>
    public Dictionary<string, double> SheetPercents { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // ---- build, by game ID
    /// <summary>Talent IDs, in the order they were picked.</summary>
    public List<int> Talents { get; set; } = new();
    public List<BuildEntry> Traits { get; set; } = new();
    public List<BuildEntry> Blessings { get; set; } = new();
    public Dictionary<GemColor, double> GemPower { get; set; } = new();
    /// <summary>The set-bonus ID of every gear piece that belongs to a set (two pieces of a set give its bonus).</summary>
    public List<int> SetBonusIds { get; set; } = new();
    /// <summary>Item IDs of the legendary items worn.</summary>
    public List<int> LegendaryItemIds { get; set; } = new();
    /// <summary>The weapon's ability ID (the weapon tree's ability, e.g. 1558 for Asha's Chronoshift Spire).</summary>
    public int WeaponAbilityId { get; set; }

    /// <summary>The date of the game to simulate (a logged fight's date) for balance changes by date; null for the current game.</summary>
    public DateTime? GameDate { get; set; }
}
