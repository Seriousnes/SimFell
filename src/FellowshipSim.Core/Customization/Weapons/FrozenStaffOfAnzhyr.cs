using FellowshipSim.Core.Customization.WeaponAbilities;

namespace FellowshipSim.Core.Customization.Weapons;

/// <summary>Frozen Staff of An'zhyr (Rime's version, item 5320): its weapon ability is Icicles of An'zhyr.</summary>
public class FrozenStaffOfAnzhyr : Weapon
{
    public const string WeaponName = "Frozen Staff of An'zhyr";

    public override int WeaponAbilityId => IciclesOfAnzhyr.AbilityId;
    public override string Name => WeaponName;

    public override void Equip(Hero hero)
    {
        Ability = new IciclesOfAnzhyr(hero);
        base.Equip(hero);
    }
}
