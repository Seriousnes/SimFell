using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>Blessing 699: start dungeons with Spirit Points (no effect on damage).</summary>
public class TheHerald : Blessing
{
    public override string Name => "The Herald";
    public override void Bind(Hero hero) { }
}
