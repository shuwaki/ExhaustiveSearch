using DigitalWorxpaces.Worxpace.Base;

namespace DigitalWorxpaces.Utilities.Search;

/// <summary>
/// Represents a scored name match while preserving the caller's concrete object type.
/// </summary>
/// <typeparam name="T">The type of named business object.</typeparam>
/// <param name="Item">The original object supplied by the caller.</param>
/// <param name="Score">
/// The relevance score, where a larger value is a stronger match. Direct-match tiers
/// range from 80 through 100; fuzzy matches cannot exceed 70.
/// </param>
/// <param name="MatchKind">The rule that produced the match.</param>
public sealed record SearchResult<T>(T Item, double Score, MatchKind MatchKind)
    where T : INamedBusinessObject;
