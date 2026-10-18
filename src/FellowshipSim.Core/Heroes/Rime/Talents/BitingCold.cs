using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>Talent 51 / effect 1372: "The power of your critical strikes is increased by 10%." (CritMultiplier x1.1)</summary>
public class BitingCold : Talent
{
    public const int EffectId = 1372;
    public override string Name => "Biting Cold";
    public override int Id => 51;
    public override void Bind(Hero hero) => hero.PassiveAuras.Add(AuraDef.FromEffect(hero.Data, EffectId));
}
