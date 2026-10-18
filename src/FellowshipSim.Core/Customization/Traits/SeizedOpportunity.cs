using System.Globalization;
using System.Text.RegularExpressions;
using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Customization.Gems;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 4: every 20 critical strikes (counted by effect 2111, not while the buff is active) grant Critical Strike
/// rating for 12s (effect 2110).
/// </summary>
public class SeizedOpportunity : Trait
{
    public const int CounterEffectId = 2111;
    public const int BuffEffectId = 2110;
    public const double DurationSeconds = 12.0;
    public override string Name => "Seized Opportunity";

    public override void Bind(Hero hero)
    {
        var counter = hero.DefineAura(CounterEffectId);
        var buff = hero.DefineAura(BuffEffectId)
            .WithDuration(DurationSeconds)
            .WithModifiers(StatModifier.Add(StatType.CritRating, RankValue(hero)));
        hero.OnDamageDealt += dmg =>
        {
            if (!dmg.IsCrit || hero.Auras.HasAura(buff, dmg.Timestamp)) return;
            var stacks = hero.Auras.Apply(counter, dmg.Timestamp).Stacks;
            if (stacks >= counter.MaxStacks)
            {
                hero.Auras.Remove(counter, dmg.Timestamp);
                hero.Auras.Apply(buff, dmg.Timestamp);
            }
        };
    }
}
