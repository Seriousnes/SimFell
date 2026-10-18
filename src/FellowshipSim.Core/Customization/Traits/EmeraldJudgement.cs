using System.Globalization;
using System.Text.RegularExpressions;
using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Customization.Gems;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 34 / trait-power 376: damage has a chance (rank PPM) to conjure an Emerald Spike for 800% Main Stat damage
/// (effect 93), which also triggers the Emerald gem's First Strike.
/// </summary>
public class EmeraldJudgement : Trait
{
    public const int TraitPowerId = 376;
    public const int DamageEffectId = 93;
    public const string ProcKey = "emerald_judgement";
    /// <summary>The spike erupts from the ground a moment after the proc (logs: about 0.5s).</summary>
    public const double SpikeDelaySeconds = 1.0;
    public override string Name => "Emerald Judgement";

    public override void Bind(Hero hero)
    {
        var rppm = new RealPpm(ProcKey, RankValue(hero));
        int logId = GameData.EffectLogId(DamageEffectId);
        string name = hero.Data.Effect(DamageEffectId).Name;
        var match = Regex.Match(hero.Data.TraitPower(TraitPowerId).Description, @"(\d+)% Main Stat damage");
        double coefficient = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) / 100.0;

        hero.OnDamageDealt += dmg =>
        {
            if (dmg.Target == null || dmg.Target.IsSleeping || dmg.SimContext == null || dmg.AbilityId == logId) return;
            var ctx = dmg.SimContext;
            // The spike erupts under the enemy whose hit triggered it.
            var target = dmg.Target;
            if (!rppm.Roll(hero, ctx, logId, SpikeDelaySeconds, target)) return;

            // The spike also triggers First Strike, when it lands (combat logs: First Strike refreshes with the spike's hit).
            var firstStrike = hero.Gems.OfType<Emerald>().FirstOrDefault()?.FirstStrike;
            ctx.ApplyDamage(hero, target, logId, name, canCrit: true, c => new SimulationContext.HitNumbers(
                coefficient * hero.PrimaryAttribute(c.CurrentTime) * (1.0 + hero.Expertise(c.CurrentTime))
                    * hero.DamageScale(c.CurrentTime) * hero.DamageMultiplierAgainst(target, c.CurrentTime),
                hero.CritChanceAgainst(target, c.CurrentTime),
                hero.CritMultiplier(c.CurrentTime)),
                onLanded: firstStrike == null ? null : (c, dealt) => { if (dealt > 0.0) hero.Auras.Apply(firstStrike, c.CurrentTime); });
        };
    }
}
