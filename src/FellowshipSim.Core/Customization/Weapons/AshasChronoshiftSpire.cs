using FellowshipSim.Core.Customization.WeaponAbilities;

namespace FellowshipSim.Core.Customization.Weapons;

public class AshasChronoshiftSpire : Weapon
{
    public const int WeaponId = 5326;
    public const string WeaponName = "Asha's Chronoshift Spire";

    public override int Id => WeaponId;
    public override string Name => WeaponName;

    public override void Equip(Hero hero)
    {
        Ability = new Chronoshift(hero);
        base.Equip(hero);
    }
}
