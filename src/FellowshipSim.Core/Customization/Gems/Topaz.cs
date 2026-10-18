using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Gems;

/// <summary>Not modeled yet: every unlocked node is reported.</summary>
public class Topaz : Gem
{
    public override GemColor Color => GemColor.Topaz;
    protected override bool BindNode(Hero hero, GemNode node) => false;
}
