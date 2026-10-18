using System.Globalization;
using System.Text.RegularExpressions;
using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Customization.Gems;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 42 / effect 2141 ("Trait : Focused Haste"): targeted offensive abilities grant Haste rating for 8s, stacking to 5;
/// lost when a targeted offensive ability is used on another enemy.
/// </summary>
public class HuntersFocus : Trait
{
    public const int EffectId = 2141;
    public const double DurationSeconds = 8.0;
    public override string Name => "Hunter's Focus";

    public override void Bind(Hero hero)
    {
        var aura = hero.DefineAura(EffectId)
            .WithDuration(DurationSeconds)
            .WithModifiers(StatModifier.Add(StatType.HasteRating, RankValue(hero)));
        EnemyTarget? focus = null;
        hero.OnCastCompleted += (ability, target, now) =>
        {
            if (target == null || ability.BaseCoefficient <= 0) return;
            if (focus != null && focus != target) hero.Auras.Remove(aura, now);
            focus = target;
            hero.Auras.Apply(aura, now);
        };
    }
}
