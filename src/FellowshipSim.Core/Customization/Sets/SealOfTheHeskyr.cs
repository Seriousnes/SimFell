using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Sets;

/// <summary>Set bonus 5638: +25% gem power (in the gem powers reported by the log).</summary>
public class SealOfTheHeskyr : SetBonus
{
    public override string Name => "Seal of the Heskyr";
    public override void Bind(Hero hero) { }
}
