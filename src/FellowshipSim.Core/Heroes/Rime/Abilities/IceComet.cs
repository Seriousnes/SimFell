namespace FellowshipSim.Core.Heroes.Rime.Abilities;

/// <summary>
/// Ability 1018. Costs 2 Winter Orbs. Also the free comets of Cold Shower and Avalanche, which
/// stay plain Ice Comets even with Icy Talons (per the user); see <see cref="FreeCast"/>. Rising Talons (Icy Talons) is built on it.
/// </summary>
public class IceComet : RimeAbility
{
    public const int AbilityId = 1018;
    public const string AbilityName = "Ice Comet";
    public const double Coefficient = 5.285;
    public const int DefaultWinterOrbCost = 2;

    /// <summary>hero_data Ice Comet TargetCountThreshold 12: damage falls off as sqrt(12/N) beyond 12 targets.</summary>
    public const double TargetThreshold = 12;

    /// <summary>
    /// Ability 2227 ("Ice Comet", GA_Rime_Util_OnTargetPulsatingAoe): the ID free comets deal their damage under.
    /// UNVERIFIED: no log we have shows a free comet's damage.
    /// </summary>
    public const int FreeCometDamageId = 2227;

    /// <summary>An Ice Comet was cast at a target: Ice Comet, Rising Talons, or Cold Shower's free comet (Avalanche).</summary>
    public event Action<SimulationContext, EnemyTarget>? CometCast;

    public IceComet(Rime owner) : this(owner, AbilityId, AbilityName)
    {
        BaseCoefficient = Coefficient;
        AoeTargetThreshold = TargetThreshold;
    }

    /// <summary>For abilities built on Ice Comet (Rising Talons), which set their own values.</summary>
    protected IceComet(Rime owner, int id, string name) : base(owner, id, name, AbilityCategory.Power) { }

    public override bool CanCast(SimulationContext ctx, out string? reason) =>
        MeetsRequirements(out reason) && base.CanCast(ctx, out reason);

    /// <summary>Talent and Winter Orb requirements, checked before the cooldown.</summary>
    protected virtual bool MeetsRequirements(out string? reason)
    {
        if (Rime.HasIcyTalons)
        {
            reason = $"{Name} is replaced by Rising Talons (Icy Talons)";
            return false;
        }
        if (Rime.WinterOrbs < DefaultWinterOrbCost)
        {
            reason = $"{Name} needs {DefaultWinterOrbCost} Winter Orbs (have {Rime.WinterOrbs})";
            return false;
        }
        reason = null;
        return true;
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        Rime.SpendWinterOrbs(DefaultWinterOrbCost, ctx);
        Strike(ctx, target, DamageLogId);
        RaiseCometCast(ctx, target);
    }

    /// <summary>
    /// A free comet from a talent's proc (<paramref name="source"/>): no cost, and it resolves as an Ice Comet cast, so
    /// talents that add to the next Ice Comet apply. With <paramref name="countsAsCast"/> it also raises
    /// <see cref="CometCast"/> (Cold Shower's comet can trigger Avalanche; Avalanche's own comets cannot).
    /// </summary>
    public void FreeCast(SimulationContext ctx, EnemyTarget target, object source, bool countsAsCast)
    {
        ResolveCast(ctx, target, source, () => Strike(ctx, target, FreeCometDamageId));
        if (countsAsCast) RaiseCometCast(ctx, target);
    }

    protected void RaiseCometCast(SimulationContext ctx, EnemyTarget target)
    {
        if (target != null) CometCast?.Invoke(ctx, target);
    }

    /// <summary>One comet impact on <paramref name="target"/> and all enemies around it.</summary>
    private void Strike(SimulationContext ctx, EnemyTarget target, int damageId)
    {
        var enemies = ctx.ActiveEnemies.ToList();
        if (enemies.Count == 0 && target is { IsSleeping: false }) enemies.Add(target);
        DealAoeDamage(ctx, enemies, overrideId: damageId);
    }
}
