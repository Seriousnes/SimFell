using System.Globalization;
using System.Text.RegularExpressions;
using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Customization.Gems;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Traits;

// Rank values come from the datamine "trait" records (rankRuns); effects from the named effect records.

/// <summary>Trait 51 / effect 2174: the weapon ability grants Main Stat for a fraction of its default cooldown.</summary>
public class MartialInitiative : Trait
{
    public const int EffectId = 2174;
    public override string Name => "Martial Initiative";

    public override void Bind(Hero hero)
    {
        var aura = hero.DefineAura(EffectId);
        double fraction = RankValue(hero);
        hero.OnCastCompleted += (ability, _, now) =>
        {
            if (ability != hero.WeaponAbility) return;
            hero.Auras.Apply(aura.WithDuration(fraction * ability.Cooldown.TotalSeconds), now);
        };
    }
}
