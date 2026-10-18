using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Heroes.Rime;
using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Customization.Legendaries;

/// <summary>
/// Legendary power 398: Winter's Blessing grants 2 stacks of Undulating Spirit (effect 2171); the next ability that
/// spends Winter Orbs consumes a stack and always triggers a Spirit Refund (handled in <see cref="Rime.SpendWinterOrbs"/>).
/// </summary>
public class UndulatingSpirit : Legendary
{
    public const int StacksGranted = 2;
    public override string Name => "Undulating Spirit";

    public override void Bind(Hero hero)
    {
        if (hero is not Rime rime) return;
        hero.OnCastCompleted += (ability, _, now) =>
        {
            if (ability is WintersBlessing) rime.Auras.Apply(rime.Aura.UndulatingSpirit, now, setStacks: StacksGranted);
        };
    }
}
