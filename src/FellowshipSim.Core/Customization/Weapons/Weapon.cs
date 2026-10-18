using FellowshipSim.Core.Domain;

namespace FellowshipSim.Core.Customization.Weapons;

public abstract class Weapon
{
    public abstract int Id { get; }
    public abstract string Name { get; }
    public virtual Ability? Ability { get; protected set; }
    public int ItemLevel { get; set; }

    public virtual void Equip(Hero hero)
    {
        hero.EquippedWeapon = this;
        if (Ability != null)
        {
            hero.WeaponAbility = Ability;
            if (!hero.Abilities.Any(a => a.Name.Equals(Ability.Name, StringComparison.OrdinalIgnoreCase)))
            {
                hero.Abilities.Add(Ability);
            }
        }
    }
}

