using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>Talent 189: Flight of the Navir grants 2 stacks of Navir's Keeper (free Cold Snaps).</summary>
public class NavirsKeeper : Talent
{
    public const int StacksGranted = 2;
    public override string Name => "Navir's Keeper";
    public override int Id => 189;
    public override void Bind(Hero hero) { }
}
