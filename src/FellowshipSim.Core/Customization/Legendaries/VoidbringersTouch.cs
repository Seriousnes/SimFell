using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Heroes.Rime;
using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Customization.Legendaries;

public class VoidbringersTouch : Legendary
{
    public override string Name => "Voidbringer's Touch";
    public override bool IsModeled => false;
    public override void Bind(Hero hero) { }
}
