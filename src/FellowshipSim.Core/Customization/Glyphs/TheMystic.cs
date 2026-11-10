namespace FellowshipSim.Core.Customization.Glyphs;

/// <summary>
/// Glyph 706: "Your POWER abilities gain increased power by rank for every step of Spirit you have, up to a Spirit cap"
/// (<see cref="SPIRIT_AMOUNT_PER_POWER_IN_PERCENTAGE"/>, <see cref="SPIRIT_THRESHOLD"/>).
/// </summary>
public partial class TheMystic : Glyph
{
    /// <summary>
    /// Tooltip: "0.3% / 0.5% / 0.8% / 1.2% increased power" per step, as fractions (the game data's
    /// <see cref="POWER_VALUES_PER_SPIRIT_AMOUNT_IN_PERCENTAGE_BY_RANK"/> is in percent).
    /// </summary>
    public static readonly double[] PowerPerStepByRank = [0.003, 0.005, 0.008, 0.012];

    public override int Id => GlyphId;
    public override string Name => GlyphName;

    public override void Bind(Hero hero)
    {
        double perStep = ForRank(PowerPerStepByRank);
        hero.AbilityDamageMultipliers.Add((ability, now) =>
            ability.Category == AbilityCategory.Power
                ? 1.0 + perStep * (Math.Min(hero.SpiritPercent(now), SPIRIT_THRESHOLD) * 100.0 / SPIRIT_AMOUNT_PER_POWER_IN_PERCENTAGE)
                : 1.0);
    }
}
