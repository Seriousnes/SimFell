using FellowshipSim.Core.Customization.Augmentations;

namespace FellowshipSim.Core.Customization.ProtectiveEnchants;

/// <summary>
/// A protective enchant: the necklace's augmentation (ItemTrait.TraitCategory.Necklace). Each one is defensive or
/// utility and has no effect on damage, so the engine leaves it out.
/// </summary>
public abstract class ProtectiveEnchant : Augmentation
{
    public override void Bind(Hero hero) { }
}
