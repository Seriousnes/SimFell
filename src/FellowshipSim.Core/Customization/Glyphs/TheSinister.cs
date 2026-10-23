namespace FellowshipSim.Core.Customization.Glyphs;

/// <summary>
/// Glyph 701: "Your [CORE] abilities have 1% / 2% / 3% / 4% increased Critical Strike chance and 5% / 10% / 15% / 20%
/// increased Critical Strike power." UNVERIFIED: no log has this glyph; crit power multiplies the crit multiplier
/// (like Biting Cold).
/// </summary>
public class TheSinister : Glyph
{
    public const int GlyphId = 701;
    public const string GlyphName = "The Sinister";
    public static readonly double[] CritChanceByRank = [0.01, 0.02, 0.03, 0.04];
    public static readonly double[] CritPowerByRank = [0.05, 0.10, 0.15, 0.20];

    public override int Id => GlyphId;
    public override string Name => GlyphName;

    public override void Bind(Hero hero)
    {
        foreach (var ability in hero.Abilities.Where(a => a.Category == AbilityCategory.Core))
        {
            ability.CritChanceBonus += ForRank(CritChanceByRank);
            ability.CritPowerMultiplier *= 1.0 + ForRank(CritPowerByRank);
        }
    }
}
