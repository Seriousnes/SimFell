using FellowshipSim.Core.Domain;

namespace FellowshipSim.Core.Customization.Relics;

public abstract class Relic
{
    public abstract string Name { get; }
    public abstract void Bind(Hero hero);
}

public class AlzeracsEssence : Relic
{
    public override string Name => "Alzerac's Essence";
    public override void Bind(Hero hero) { }
}

