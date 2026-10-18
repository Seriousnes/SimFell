using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Gems;

/// <summary>Not modeled yet: every unlocked node is reported.</summary>
public class Ruby : Gem
{
    public override GemColor Color => GemColor.Ruby;
    protected override bool BindNode(Hero hero, GemNode node) => false;
}
