namespace FellowshipSim.Core.Engine;

/// <summary>
/// Game attributes, named as in the datamine (effect "modifiers[].attribute").
/// </summary>
public enum StatType
{
    PrimaryAttribute,
    PrimaryAttributeMultiplier,
    Stamina,
    CritRating,
    CritChance,
    CritMultiplier,
    HasteRating,
    Haste,
    ExpertiseRating,
    Expertise,
    SpiritRating,
    Spirit,
    DamageScale,
    CooldownReduction,
    CooldownRecovery,
    IncomingDamageMultiplier,
}

/// <summary>Modifier operations, named as in the datamine (effect "modifiers[].op").</summary>
public enum ModifierOp
{
    /// <summary>Adds the value ("+X").</summary>
    Additive,
    /// <summary>"X increased": all such modifiers add their (value - 1) together, then multiply once (datamine op "Multiplicitive").</summary>
    MultiplyAdditive,
    /// <summary>"X more": each multiplies the result (datamine op "MultiplyCompound").</summary>
    MultiplyCompound,
}

/// <summary>One modifier, per stack of the aura carrying it.</summary>
public readonly record struct StatModifier(StatType Type, double Value, ModifierOp Op = ModifierOp.Additive)
{
    public static StatModifier Add(StatType type, double value) => new(type, value, ModifierOp.Additive);
    public static StatModifier Increased(StatType type, double multiplier) => new(type, multiplier, ModifierOp.MultiplyAdditive);
    public static StatModifier More(StatType type, double multiplier) => new(type, multiplier, ModifierOp.MultiplyCompound);

    public static ModifierOp ParseOp(string datamineOp) => datamineOp switch
    {
        "Additive" => ModifierOp.Additive,
        "Multiplicitive" => ModifierOp.MultiplyAdditive,
        "MultiplyCompound" => ModifierOp.MultiplyCompound,
        _ => throw new NotSupportedException($"Unknown datamine modifier op '{datamineOp}'")
    };
}

/// <summary>Accumulates modifiers for one attribute and applies them to a base value.</summary>
public struct ModifierFold
{
    private double _add;
    private double _increased;
    private double _more;
    private bool _hasMore;

    public void Accept(in StatModifier mod, int stacks)
    {
        switch (mod.Op)
        {
            case ModifierOp.Additive:
                _add += mod.Value * stacks;
                break;
            case ModifierOp.MultiplyAdditive:
                _increased += (mod.Value - 1.0) * stacks;
                break;
            case ModifierOp.MultiplyCompound:
                _more = (_hasMore ? _more : 1.0) * Math.Pow(mod.Value, stacks);
                _hasMore = true;
                break;
        }
    }

    public readonly double Apply(double baseValue) => (baseValue + _add) * (1.0 + _increased) * (_hasMore ? _more : 1.0);
}

public static class StatDiminishingReturns
{
    /// <summary>Percent per rating point (settings.json statCalculation.attributes.*.baseStatMultiplier = 0.16%).</summary>
    public const double PercentPerRating = 0.0016;

    private static readonly double[] Breakpoints = [0.10, 0.15, 0.20, 0.25];
    private static readonly double[] Multipliers = [
        1.0,
        0.98,
        0.96 * 0.98,
        0.94 * 0.96 * 0.98,
        0.92 * 0.94 * 0.96 * 0.98
    ];

    public static double Apply(double unmitigatedPercent)
    {
        if (unmitigatedPercent <= Breakpoints[0])
            return unmitigatedPercent * Multipliers[0];

        double output = Breakpoints[0] * Multipliers[0];
        double remaining = unmitigatedPercent - output;
        double currentBreakpoint = output;

        for (int i = 1; i < Breakpoints.Length; i++)
        {
            double prevBreakpoint = currentBreakpoint;
            currentBreakpoint += (Breakpoints[i] - Breakpoints[i - 1]) / Multipliers[i];
            double segment = currentBreakpoint - prevBreakpoint;

            if (remaining <= segment)
            {
                output += remaining * Multipliers[i];
                return output;
            }

            output += segment * Multipliers[i];
            remaining -= segment;
        }

        if (remaining > 0)
        {
            output += remaining * Multipliers[^1];
        }

        return output;
    }

    public static double FromRating(double rating) => Apply(rating * PercentPerRating);

    public static double CalculateEffectiveExpertise(double rating, double flatBonus = 0.0) => FromRating(rating) + flatBonus;
}

/// <summary>
/// A hero's base stats. Ratings and primary attribute come from gear; the character-sheet percentages,
/// when known (imported from a log), already include passive bonuses and are used as the base percentages.
/// </summary>
public class StatBlock
{
    public const double BaseCritChance = 0.05;
    public const double BaseCritMultiplier = 2.0;

    public double BaseIntellect { get; set; } = 342;
    public double Stamina { get; set; } = 635;
    public double Armor { get; set; } = 539;

    public double CritRating { get; set; }
    public double HasteRating { get; set; }
    public double ExpertiseRating { get; set; }
    public double SpiritRating { get; set; }

    /// <summary>Permanent percent bonuses beyond ratings (gem nodes, set bonuses), used when no sheet value is known.</summary>
    public double BonusCritChance { get; set; }
    public double BonusHaste { get; set; }
    public double BonusExpertise { get; set; }
    public double BonusSpirit { get; set; }

    /// <summary>Character-sheet percentages from the log; when set they replace the derived base percentages.</summary>
    public double? SheetCritChance { get; set; }
    public double? SheetHaste { get; set; }
    public double? SheetExpertise { get; set; }
    public double? SheetSpirit { get; set; }

    /// <summary>True when base percentages come from an imported character sheet (which already includes passive bonuses).</summary>
    public bool UsesSheetValues => SheetCritChance.HasValue;

    public double DerivedCritChance => BaseCritChance + StatDiminishingReturns.FromRating(CritRating) + BonusCritChance;
    public double DerivedHaste => StatDiminishingReturns.FromRating(HasteRating) + BonusHaste;
    public double DerivedExpertise => StatDiminishingReturns.FromRating(ExpertiseRating) + BonusExpertise;
    public double DerivedSpirit => StatDiminishingReturns.FromRating(SpiritRating) + BonusSpirit;

    public double CritPercent => SheetCritChance ?? DerivedCritChance;
    public double HastePercent => SheetHaste ?? DerivedHaste;
    public double ExpertisePercent => SheetExpertise ?? DerivedExpertise;
    public double SpiritPercent => SheetSpirit ?? DerivedSpirit;

    public double CooldownReductionPct { get; set; }

    /// <summary>
    /// Percentage for a rating stat after <paramref name="extraRating"/> more rating, keeping the base
    /// percentage (sheet or derived) and adding only the diminishing-returns-adjusted difference.
    /// </summary>
    public static double WithExtraRating(double basePercent, double baseRating, double extraRating) =>
        extraRating == 0
            ? basePercent
            : basePercent + StatDiminishingReturns.FromRating(baseRating + extraRating) - StatDiminishingReturns.FromRating(baseRating);
}
