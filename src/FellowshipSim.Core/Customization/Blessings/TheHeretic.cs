namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>
/// Blessing 708: "Each time you use a [CORE] ability, there is a 20% chance you drain 325% / 520% / 832% / 1331% Main Stat
/// health from the nearest enemy, healing you for the same amount." (Heretic's Feast, effects 3187/3188). UNVERIFIED: no
/// log has this blessing; the drain is taken as a hit on the ability's target under effect 3187.
/// </summary>
public class TheHeretic : Blessing
{
    public const int BlessingId = 708;
    public const string BlessingName = "The Heretic";
    public const int DamageEffectId = 3187;
    public const string DamageName = "Heretic's Feast";
    public const double ProcChance = 0.2;
    public const string ProcKey = "the_heretic";
    public static readonly double[] CoefficientByRank = [3.25, 5.2, 8.32, 13.31];

    public override int Id => BlessingId;
    public override string Name => BlessingName;

    public override void Bind(Hero hero)
    {
        var drain = new Drain(hero, ForRank(CoefficientByRank));
        hero.OnCastCompleted += (ability, target, _) =>
        {
            if (ability.Category != AbilityCategory.Core || target == null || hero.SimContext is not { } ctx) return;
            if (ctx.Outcomes.RollProc(ProcKey, ProcChance, ctx, target)) drain.DealDamage(ctx, target);
        };
    }

    private sealed class Drain : Ability
    {
        public Drain(Hero owner, double coefficient) : base(owner, LogIds.Effect(DamageEffectId), DamageName)
        {
            BaseCoefficient = coefficient;
        }

        public override void Execute(SimulationContext ctx, EnemyTarget target) { }
    }
}
