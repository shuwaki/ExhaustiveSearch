namespace ExhaustiveSearch;

/// <summary>
/// Configures a search operation.
/// </summary>
public sealed record SearchOptions
{
    /// <summary>
    /// Gets the maximum number of results, or <see langword="null"/> to return all results.
    /// </summary>
    public int? MaximumResults { get; init; }

    /// <summary>
    /// Gets the minimum inclusive relevance score. Valid values range from 0 through 100.
    /// </summary>
    public double MinimumScore { get; init; }

    /// <summary>
    /// Gets a value indicating whether ordered-subsequence fuzzy matches are included.
    /// </summary>
    public bool IncludeFuzzyMatches { get; init; } = true;
}
