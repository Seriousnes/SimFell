using System.Collections.Concurrent;
using FellowshipSim.Core.APL;

namespace FellowshipSim.Engine.Services;

/// <summary>Each hero's <see cref="AplVocabulary"/>, built once per hero.</summary>
public sealed class Vocabularies
{
    private readonly ConcurrentDictionary<string, AplVocabulary> _vocabularies = new(StringComparer.OrdinalIgnoreCase);

    public AplVocabulary For(string hero) => _vocabularies.GetOrAdd(hero, AplVocabulary.For);
}
