using System.Text.Json.Serialization;

namespace FellowshipSim.Engine;

/// <summary>Completed fights of a Monte Carlo sim of <see cref="Fights"/> fights.</summary>
public sealed record SimProgress(int CompletedFights, int Fights)
{
    [JsonIgnore]
    public double Fraction => Fights == 0 ? 1 : (double)CompletedFights / Fights;
}
