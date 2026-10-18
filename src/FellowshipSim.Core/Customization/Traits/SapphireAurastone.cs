namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 32: "Your Spirit Ability causes you to conjure a Sapphire Aurastone near you or your selected target on activation. The Sapphire Aurastone lasts as long as the Spirit of Heroism effect from your Spirit Ability is active on you. While active, 3.4% / 6.3% / 9.1% / 12% of all damage you deal is accumulated into the Sapphire Aurastone and released in a pulse of damage, evenly split between nearby enemies every 3 seconds. While active, 4.6% / 8.4% / 12% / 16% of all healing you do is accumulated into the Sapphire Aurastone and released in a pulse of healing, evenly split between nearby allies every 3 seconds. Each pulse empties the accumulated Damage or Healing stored within it."
/// Not modeled yet (reported, no effect).
/// </summary>
public class SapphireAurastone : UnmodeledTrait
{
    public const int TraitId = 32;
    public const string TraitName = "Sapphire Aurastone";
    public override int Id => TraitId;
    public override string Name => TraitName;
}
