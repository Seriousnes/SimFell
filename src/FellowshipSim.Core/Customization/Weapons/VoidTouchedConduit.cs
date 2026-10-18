using FellowshipSim.Core.Customization.WeaponAbilities;

namespace FellowshipSim.Core.Customization.Weapons;

/// <summary>Void-touched Conduit (Rime's version, items 5346 and 5372): its weapon ability is Voidbringer's Touch.</summary>
public class VoidTouchedConduit : Weapon
{
    public const string WeaponName = "Void-touched Conduit";

    public override int WeaponAbilityId => VoidbringersTouch.AbilityId;
    public override string Name => WeaponName;

    public override void Equip(Hero hero)
    {
        Ability = new VoidbringersTouch(hero);
        base.Equip(hero);
    }
}
