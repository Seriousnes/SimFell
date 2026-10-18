namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>
/// Blessing 711: "Your [BASIC] abilities increases the power of your next [POWER] ability by 1.5% / 2.5% / 4.0% / 6.0%,
/// stacking up to 5 times." (effect 3192). UNVERIFIED: no log has this blessing; the next POWER cast uses up every stack.
/// </summary>
public class TheIntrepid : Blessing
{
    public const int BlessingId = 711;
    public const string BlessingName = "The Intrepid";
    public const int BuffEffectId = 3192;
    public const int MaxStacks = 5;
    public static readonly double[] PowerPerStackByRank = [0.015, 0.025, 0.04, 0.06];

    public static readonly AuraDef Buff = AuraDef.Effect(BlessingName, BuffEffectId, maxStacks: MaxStacks);

    public override int Id => BlessingId;
    public override string Name => BlessingName;

    public override void Bind(Hero hero)
    {
        double perStack = ForRank(PowerPerStackByRank);
        hero.OnCastCompleted += (ability, _, now) =>
        {
            if (ability.Category == AbilityCategory.Basic) hero.Auras.Apply(Buff, now);
        };
        hero.OnCastResolving += cast =>
        {
            if (cast.Ability.Category != AbilityCategory.Power) return;
            int stacks = hero.Auras.GetStacks(Buff, cast.Time);
            if (stacks == 0) return;
            hero.Auras.Remove(Buff, cast.Time);
            cast.DamageMultiplier *= 1.0 + perStack * stacks;
        };
    }
}
