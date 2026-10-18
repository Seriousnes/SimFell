using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Sets;

public class DraconicMight : SetBonus
{
    public override string Name => "Draconic Might";
    public override IReadOnlyList<string> UnmodeledParts => ["entire bonus"];
    public override void Bind(Hero hero) { }
}
