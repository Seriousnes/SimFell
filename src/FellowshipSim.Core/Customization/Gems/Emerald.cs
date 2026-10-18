using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Gems;

public class Emerald : Gem
{
    /// <summary>The engine-internal marker that an enemy has already triggered First Strike.</summary>
    public static readonly AuraDef FirstStrikeTriggered = AuraDef.Internal("first_strike_triggered", 0);

    public override GemColor Color => GemColor.Emerald;

    /// <summary>The First Strike aura of the unlocked rank, if any (Emerald Judgement also triggers it).</summary>
    public AuraDef? FirstStrike { get; private set; }

    protected override bool BindNode(Hero hero, GemNode node)
    {
        switch (node.Slot)
        {
            case 1: // First Strike: Expertise for 15s when dealing damage to an enemy for the first time.
                FirstStrike = hero.DefineAura(node.GrantedEffectId ?? throw new InvalidOperationException($"{node.Name} grants no effect"));
                hero.OnDamageDealt += dmg =>
                {
                    if (dmg.Target == null || dmg.Target.Auras.HasAura(FirstStrikeTriggered, dmg.Timestamp)) return;
                    dmg.Target.Auras.Apply(FirstStrikeTriggered, dmg.Timestamp);
                    hero.Auras.Apply(FirstStrike, dmg.Timestamp);
                };
                return true;
            case 2: // Vanguard's Resolve: flat Stamina/Expertise rating (in the sheet ratings).
                return true;
            case 4: // Tactician's Accumen: Expertise.
                hero.Stats.BonusExpertise += node.Value("ExpertiseBonus");
                return true;
            case 5: // Blessing of the Commander: ability cooldown reduction.
                hero.Stats.CooldownReductionPct += node.Value("CooldownReduction");
                return true;
            default: // Sentinel's Bastion (shield) is defensive.
                return false;
        }
    }
}
