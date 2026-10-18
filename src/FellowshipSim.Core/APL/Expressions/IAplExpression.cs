using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.APL.Expressions;

public interface IAplExpression
{
    bool Evaluate(Hero hero, SimulationContext ctx);
    double EvaluateNumeric(Hero hero, SimulationContext ctx);
}

