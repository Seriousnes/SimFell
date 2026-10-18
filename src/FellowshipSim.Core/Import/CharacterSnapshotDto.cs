namespace FellowshipSim.Core.Import;

public class CharacterSnapshotDto
{
    public string Name { get; set; } = "sww";
    public string HeroClass { get; set; } = "Rime";
    public int Level { get; set; } = 80;

    public double Intellect { get; set; } = 342;
    public double Stamina { get; set; } = 635;
    public double Armor { get; set; } = 539;

    public double HasteRating { get; set; } = 161;
    public double ExpertiseRating { get; set; } = 100;
    public double CritRating { get; set; } = 141;
    public double Spirit { get; set; } = 127;

    public double BaseCritPercent { get; set; } = 0.289;
    public double BaseHastePercent { get; set; } = 0.2473;
    public double VersatilityPercent { get; set; } = 0.0328;

    public Dictionary<string, double> Gems { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sapphire_power"] = 1500,
        ["amethyst_power"] = 625,
        ["emerald_power"] = 625
    };

    public Dictionary<string, int> Blessings { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["the_herald"] = 3,
        ["the_monarch"] = 1,
        ["the_mystic"] = 4,
        ["the_philosopher"] = 4,
        ["the_trickster"] = 4
    };

    public Dictionary<string, int> WeaponTraits { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["martial_initiative"] = 2,
        ["amethyst_splinters"] = 4,
        ["visions_of_grandeur"] = 2,
        ["emerald_judgement"] = 1
    };

    public List<string> Talents { get; set; } = new()
    {
        "glacial_assault",
        "navirs_keeper",
        "cascading_blitz",
        "greater_glacial_blast",
        "bursting_swallows",
        "biting_cold",
        "icy_talons",
        "burstbolter",
        "chilling_finesse",
        "wisdom_of_the_north"
    };

    public List<string> Sets { get; set; } = new()
    {
        "torment_of_baelaurum",
        "drakheims_absolution",
        "seal_of_the_heskyr",
        "deaths_grasp"
    };

    public string Legendary { get; set; } = "undulating_spirit";
    public string Weapon { get; set; } = "ashas_chronoshift_spire";

    /// <summary>Character-sheet percentages as logged (crit, haste, expertise, spirit), for verifying stat derivation.</summary>
    public Dictionary<string, double> SheetPercents { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The date of the game to simulate (a logged fight's date); null for the current game.</summary>
    public DateTime? GameDate { get; set; }

    public static CharacterSnapshotDto CreateDefaultRime() => new();
}

