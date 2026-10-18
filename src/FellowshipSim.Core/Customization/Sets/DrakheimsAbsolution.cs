using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Sets;

/// <summary>Set bonus 691 / effect 1993: the Spirit ability grants +20% Intellect for 20 seconds.</summary>
public class DrakheimsAbsolution : SetBonus
{
    public const int EffectId = 1993;
    public override string Name => "Drakheim's Absolution";

    public override void Bind(Hero hero)
    {
        var aura = hero.DefineAura(EffectId);
        hero.OnCastCompleted += (ability, _, now) =>
        {
            if (ability.Category == "Spirit") hero.Auras.Apply(aura, now);
        };
    }
}
