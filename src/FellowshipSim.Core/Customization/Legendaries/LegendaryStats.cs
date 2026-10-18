namespace FellowshipSim.Core.Customization.Legendaries;

/// <summary>Stats every legendary item carries, whatever its power.</summary>
public static class LegendaryStats
{
    public const int StrandOfEternityEffectId = 3087;
    /// <summary>+10% cooldown recovery.</summary>
    public const double StrandOfEternityRecovery = 1.1;
    public const int MoteOfCreationEffectId = 3085;
    /// <summary>+10% primary stat (already in the character sheet's primary stat when imported from a log).</summary>
    public const double MoteOfCreationPrimaryStat = 0.1;

    public static readonly AuraDef StrandOfEternity = AuraDef.Effect("Strand of Eternity", StrandOfEternityEffectId,
        modifiers: StatModifier.Increased(StatType.CooldownRecovery, StrandOfEternityRecovery));

    public static readonly AuraDef MoteOfCreation = AuraDef.Effect("Mote of Creation", MoteOfCreationEffectId,
        modifiers: StatModifier.Add(StatType.PrimaryAttributeMultiplier, MoteOfCreationPrimaryStat));
}
