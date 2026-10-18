namespace FellowshipSim.Core.Engine;

public class RealPpm
{
    public double BasePpm { get; }
    public SimTime LastProcAttemptTime { get; private set; } = SimTime.Zero;

    public string ProcKey { get; }

    public RealPpm(string procKey, double basePpm)
    {
        ProcKey = procKey;
        BasePpm = basePpm;
    }

    public bool Roll(Hero hero, SimulationContext ctx, int evidenceLogId = 0, double effectDelaySeconds = 0.0, EnemyTarget? evidenceTarget = null)
    {
        double timeSinceLastSeconds = (ctx.CurrentTime - LastProcAttemptTime).TotalSeconds;
        LastProcAttemptTime = ctx.CurrentTime;

        // Haste scales RPPM proc frequency
        double hastedPpm = BasePpm * (1.0 + hero.GetHaste(ctx.CurrentTime));
        double procChance = Math.Clamp((hastedPpm * timeSinceLastSeconds) / 60.0, 0.0, 1.0);

        return ctx.Outcomes.RollProc(ProcKey, procChance, ctx, evidenceLogId, effectDelaySeconds, evidenceTarget);
    }
}

