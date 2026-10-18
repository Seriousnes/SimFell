using System.Globalization;
using System.Text.RegularExpressions;
using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Customization.Gems;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Traits;

// Traits below have no engine model yet (defensive, healing, or not implemented); they are reported, not guessed.
public class Kindling : UnmodeledTrait { public override string Name => "Kindling"; }
