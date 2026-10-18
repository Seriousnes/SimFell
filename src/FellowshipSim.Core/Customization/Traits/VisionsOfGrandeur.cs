using System.Globalization;
using System.Text.RegularExpressions;
using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Customization.Gems;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Traits;

/// <summary>Trait 47: activating the Spirit ability reduces the weapon ability's cooldown by a fraction of its default cooldown.</summary>
public class VisionsOfGrandeur : Trait
{
    public override string Name => "Visions of Grandeur";

    public override void Bind(Hero hero)
    {
        double fraction = RankValue(hero);
        hero.OnCastCompleted += (ability, _, now) =>
        {
            if (ability.Category != "Spirit" || hero.WeaponAbility == null) return;
            hero.ReduceCooldown(hero.WeaponAbility.Name, SimTime.FromSeconds(fraction * hero.WeaponAbility.Cooldown.TotalSeconds), now);
        };
    }
}
