using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

/// <summary>Ability 1028. Costs 2 Winter Orbs.</summary>
public class GlacialBlast : GlacialBlastFamily
{
    public const int AbilityId = 1028;
    public const string AbilityName = "Glacial Blast";
    public const double Coefficient = 10.24;
    public const double CastTimeSeconds = 2.0;
    public const int DefaultWinterOrbCost = 2;

    /// <summary>Seconds added to the cast time (Greater Glacial Blast).</summary>
    public double ExtraCastTimeSeconds { get; set; }

    public GlacialBlast(Rime owner) : base(owner, AbilityId, AbilityName)
    {
        BaseCoefficient = Coefficient;
        CastTime = SimTime.FromSeconds(CastTimeSeconds);
    }

    public override SimTime GetAdjustedCastTime(SimTime now = default)
    {
        // Instant with 4 Glacial Assault stacks or during Wrath of Winter.
        if (GlacialAssaultReady || Rime.Auras.HasAura(WrathOfWinter.Buff, now)) return SimTime.Zero;
        double seconds = CastTime.TotalSeconds + ExtraCastTimeSeconds;
        // Icy Flow: "+50% Haste to you while casting" the Glacial Blast that consumes it.
        double icyFlowHaste = Rime.Auras.HasAura(IcyFlow.Buff, now) ? IcyFlow.CastHaste : 0.0;
        return SimTime.FromSeconds(seconds / (1.0 + Owner.GetHaste(now) + icyFlowHaste));
    }

    public override bool CanCast(SimulationContext ctx, out string? reason)
    {
        if (Rime.HasIcyTalons)
        {
            reason = $"{Name} is replaced by Talon Strike (Icy Talons)";
            return false;
        }
        if (Rime.WinterOrbs < DefaultWinterOrbCost && !GlacialAssaultReady)
        {
            reason = $"{Name} needs {DefaultWinterOrbCost} Winter Orbs (have {Rime.WinterOrbs})";
            return false;
        }
        return base.CanCast(ctx, out reason);
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        var hero = Rime;
        bool assault = GlacialAssaultReady;
        double multiplier = 1.0;
        if (assault)
        {
            hero.Auras.Remove(GlacialAssault.Buff, ctx.CurrentTime);
            multiplier = GlacialAssaultMultiplier;
        }
        else
        {
            hero.SpendWinterOrbs(DefaultWinterOrbCost, ctx);
        }

        double crit = hero.ConsumeSpenderCritBuffs(ctx);
        DealDamage(ctx, target, damageMultiplier: multiplier, onLanded: assault ? (c, dealt) => Explode(c, target, dealt) : null, extraCritChance: crit);
    }
}
