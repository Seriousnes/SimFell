using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>
/// Blessing 712: every 6 / 5 / 4 / 3 BASIC abilities, the target erupts, dealing that hit's damage again (effect 3193,
/// cannot crit) to all enemies around it, increased by your Critical Strike chance up to 50%. Counter: effect 3194.
/// Beyond 5 targets the eruption loses damage as sqrt(5/N) (TargetCountScaling 5, fs_tc_uploads s3/gear_data.json;
/// combat logs: e.g. a 7,818 Cold Snap crit -> 7,818 x 1.5 x sqrt(5/20) = 5,864 on 20 enemies, exactly as logged).
/// </summary>
public class TheVehement : Blessing
{
    public const int DamageEffectId = 3193;
    public const int CounterEffectId = 3194;
    public const double TargetCountScaling = 5.0;
    public override string Name => "The Vehement";

    public override void Bind(Hero hero)
    {
        int threshold = (int)RankData.EffectConstant(hero.Data, CounterEffectId, "Finesse.BasicToAoe.CounterThreshold", Rank);
        double critCap = Constant(hero, "CritChanceThreshold");
        var counter = hero.DefineAura(CounterEffectId).WithMaxStacks(threshold - 1) with { Duration = SimTime.Infinite };
        int damageId = GameData.EffectLogId(DamageEffectId);
        string damageName = hero.Data.Effect(DamageEffectId).Name;
        // The counter advances when a BASIC ability's hit lands (combat logs: Frost Bolt counts when the bolt hits, not when
        // its cast completes), and the hit that reaches the threshold erupts.
        var basicIds = hero.Abilities.Where(a => a.Category == "Basic").Select(a => a.DamageLogId).ToHashSet();
        int hits = 0;
        hero.OnDamageDealt += dmg =>
        {
            if (dmg.SimContext == null || dmg.Target == null || !basicIds.Contains(dmg.AbilityId)) return;
            hits++;
            if (hits < threshold)
            {
                hero.Auras.Apply(counter, dmg.Timestamp, setStacks: hits);
                return;
            }
            hits = 0;
            hero.Auras.Remove(counter, dmg.Timestamp);
            var ctx = dmg.SimContext;
            var enemies = ctx.Outcomes.SelectTargets(damageId, ctx.ActiveEnemies.ToList(), ctx);
            if (enemies.Count == 0) return;
            double falloff = enemies.Count > TargetCountScaling ? Math.Sqrt(TargetCountScaling / enemies.Count) : 1.0;
            double damage = dmg.Amount * (1.0 + Math.Min(hero.CritChance(dmg.Timestamp), critCap)) * falloff;
            foreach (var enemy in enemies)
            {
                ctx.ApplyDamage(hero, enemy, damage, damageId, damageName, canCrit: false, 0.0, 1.0);
            }
        };
    }
}
