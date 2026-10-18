using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Blessings;

// Values come from the datamine blessing records (descriptions and placeholder constants) and the named effects.

/// <summary>Blessing 700 / effect 3181: [POWER] abilities grant Critical Strike chance for 4 seconds.</summary>
public class TheTrickster : Blessing
{
    public const int EffectId = 3181;
    public const double DurationSeconds = 4.0;
    public override string Name => "The Trickster";

    public override void Bind(Hero hero)
    {
        var aura = hero.DefineAura(EffectId)
            .WithDuration(DurationSeconds)
            .WithModifiers(StatModifier.Add(StatType.CritChance, DescriptionValue(hero)));
        hero.OnCastStarted += (ability, _, now) =>
        {
            if (ability.Category == "Power") hero.Auras.Apply(aura, now);
        };
    }
}
