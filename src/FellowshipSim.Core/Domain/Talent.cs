namespace FellowshipSim.Core.Domain;

public abstract class Talent
{
    public abstract string Name { get; }
    public abstract int Id { get; }
    public virtual string Description => string.Empty;

    public abstract void Bind(Hero hero);
}
