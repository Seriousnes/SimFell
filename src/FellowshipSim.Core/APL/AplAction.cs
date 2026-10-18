using FellowshipSim.Core.APL.Expressions;

namespace FellowshipSim.Core.APL;

public class AplAction
{
    public string AbilityName { get; init; } = string.Empty;
    public IAplExpression? Condition { get; init; }
    public string RawLine { get; init; } = string.Empty;

    public bool CanExecute(Hero hero, SimulationContext ctx)
    {
        if (Condition != null && !Condition.Evaluate(hero, ctx))
        {
            return false;
        }

        var ability = ResolveAbility(hero);
        return ability == null || ability.CanCast(ctx, out _);
    }

    public Ability? ResolveAbility(Hero hero)
    {
        string normalized = NormalizeName(AbilityName);
        return hero.Abilities.FirstOrDefault(a =>
            NormalizeName(a.Name).Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
            NormalizeName(a.GetType().Name).Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Starts the action's ability and sets the global cooldown. Returns false if nothing was cast.</summary>
    public bool Execute(Hero hero, SimulationContext ctx)
    {
        var target = ctx.Player.CurrentTarget ?? ctx.ActiveEnemies.FirstOrDefault();
        if (target == null)
        {
            target = ctx.AddEnemy(1, 1, "Target Dummy");
            ctx.Player.CurrentTarget = target;
        }

        var ability = ResolveAbility(hero);
        if (ability == null)
        {
            hero.GlobalCooldownEndTime = ctx.CurrentTime + SimTime.FromSeconds(1.0);
            return false;
        }

        var attempt = ctx.BeginCast(hero, ability, target);
        if (!attempt.Succeeded) return false;

        if (!ability.IgnoresGcd)
        {
            var gcd = attempt.CastTime == SimTime.Zero ? SimTime.FromSeconds(1.0) : attempt.CastTime;
            hero.GlobalCooldownEndTime = ctx.CurrentTime + gcd;
        }
        return true;
    }

    private static string NormalizeName(string name)
    {
        return name.Replace("_", "").Replace(" ", "").ToLowerInvariant();
    }

    public override string ToString() =>
        Condition != null ? $"{AbilityName},if={Condition}" : AbilityName;
}
