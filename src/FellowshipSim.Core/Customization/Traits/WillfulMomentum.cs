namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 39: "Your Spirit Rating is increased by 7 / 11 / 17 / 23. Whenever you trigger a Spirit Refund your Main Stat is
/// increased by 1.5% / 2.6% / 3.7% / 4.8% for 4 seconds." (effect 2078; the logged Spirit rating already includes the
/// rating)
/// </summary>
public class WillfulMomentum : Trait
{
    public const int TraitId = 39;
    public const string TraitName = "Willful Momentum";
    public const int BuffEffectId = 2078;
    public const double BuffSeconds = 4.0;
    public static readonly double[] SpiritRatingByRank = [7, 11, 17, 23];
    public static readonly double[] MainStatByRank = [0.015, 0.026, 0.037, 0.048];

    public static readonly AuraDef Buff = AuraDef.Effect("Willful Momentum", BuffEffectId, BuffSeconds);

    public override int Id => TraitId;
    public override string Name => TraitName;

    public override void Bind(Hero hero)
    {
        if (!hero.Stats.UsesSheetValues) hero.Stats.SpiritRating += ForRank(SpiritRatingByRank);
        var buff = Buff.WithModifiers(StatModifier.Increased(StatType.PrimaryAttributeMultiplier, 1.0 + ForRank(MainStatByRank)));
        hero.OnSpiritRefund += (_, _, ctx) => hero.Auras.Apply(buff, ctx.CurrentTime);
    }
}
