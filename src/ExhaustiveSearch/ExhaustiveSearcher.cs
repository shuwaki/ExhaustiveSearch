using DigitalWorxpaces.Worxpace.Base;

namespace ExhaustiveSearch;

/// <summary>
/// Searches named business objects and orders matches by relevance.
/// </summary>
public static class ExhaustiveSearcher
{
    private const double ExactScore = 100;
    private const double ExactWordScore = 90;
    private const double WordAffixScore = 85;
    private const double SubstringScore = 80;
    private const double FuzzyBaseScore = 40;
    private const double FuzzyAdjacencyWeight = 15;
    private const double FuzzyCompactnessWeight = 10;
    private const double FuzzyWordBoundaryBonus = 5;

    /// <summary>
    /// Finds objects whose names match <paramref name="query"/>.
    /// </summary>
    /// <typeparam name="T">The caller's named business object type.</typeparam>
    /// <param name="items">The objects to search. The sequence is enumerated once.</param>
    /// <param name="query">The case-insensitive query.</param>
    /// <param name="options">Optional result limits.</param>
    /// <param name="cancellationToken">A token used to cancel enumeration and scoring.</param>
    /// <returns>
    /// Matches ordered from strongest to weakest using match position, compactness,
    /// name, and source order as deterministic tie-breakers.
    /// </returns>
    public static IReadOnlyList<SearchResult<T>> FindMatches<T>(
        IEnumerable<T> items,
        string query,
        SearchOptions? options = null,
        CancellationToken cancellationToken = default)
        where T : INamedBusinessObject
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        options ??= new SearchOptions();

        if (options.MaximumResults < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.MaximumResults,
                "MaximumResults cannot be negative.");
        }

        if (!double.IsFinite(options.MinimumScore) ||
            options.MinimumScore < 0 ||
            options.MinimumScore > ExactScore)
        {
            throw new ArgumentOutOfRangeException(
                nameof(SearchOptions.MinimumScore),
                options.MinimumScore,
                $"MinimumScore must be a finite value from 0 through {ExactScore}.");
        }

        if (string.IsNullOrWhiteSpace(query) || options.MaximumResults == 0)
        {
            return Array.Empty<SearchResult<T>>();
        }

        string q = query.ToLowerInvariant();
        var results = new List<RankedResult<T>>();
        int index = 0;

        foreach (T itemObject in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int sourceIndex = index++;

            if (itemObject is null)
            {
                throw new ArgumentException("The items sequence cannot contain null values.", nameof(items));
            }

            string item = itemObject.Name.ToLowerInvariant();

            if (item == q)
            {
                AddResult(ExactScore, MatchKind.Exact);
                continue;
            }

            IReadOnlyList<Token> parts = Tokenize(item, cancellationToken);

            Token? exactWord = parts.FirstOrDefault(p => p.Value == q);
            if (exactWord is not null)
            {
                AddResult(ExactWordScore, MatchKind.ExactWord, exactWord.Start);
                continue;
            }

            Token? prefix = parts.FirstOrDefault(
                p => p.Value.StartsWith(q, StringComparison.Ordinal));
            if (prefix is not null)
            {
                AddResult(WordAffixScore, MatchKind.WordPrefix, prefix.Start);
                continue;
            }

            Token? suffix = parts.FirstOrDefault(
                p => p.Value.EndsWith(q, StringComparison.Ordinal));
            if (suffix is not null)
            {
                AddResult(
                    WordAffixScore,
                    MatchKind.WordSuffix,
                    suffix.Start + suffix.Value.Length - q.Length);
                continue;
            }

            int substringStart = item.IndexOf(q, StringComparison.Ordinal);
            if (substringStart >= 0)
            {
                AddResult(SubstringScore, MatchKind.Substring, substringStart);
                continue;
            }

            FuzzyMatch? fuzzyMatch = options.IncludeFuzzyMatches
                ? FindBestFuzzyMatch(item, q, cancellationToken)
                : null;
            if (fuzzyMatch is not null)
            {
                AddResult(
                    fuzzyMatch.Value.Score,
                    MatchKind.Fuzzy,
                    fuzzyMatch.Value.Start,
                    fuzzyMatch.Value.GapCount);
            }

            void AddResult(double score, MatchKind matchKind, int matchStart = 0, int gapCount = 0)
            {
                if (score < options.MinimumScore)
                {
                    return;
                }

                results.Add(new(
                    new SearchResult<T>(itemObject, score, matchKind),
                    matchStart,
                    gapCount,
                    itemObject.Name,
                    sourceIndex));
            }
        }

        IEnumerable<SearchResult<T>> ordered = results
            .OrderByDescending(x => x.Result.Score)
            .ThenBy(x => x.MatchStart)
            .ThenBy(x => x.GapCount)
            .ThenBy(x => x.Name.Length)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.SourceIndex)
            .Select(x => x.Result);

        if (options.MaximumResults is int maximumResults)
        {
            ordered = ordered.Take(maximumResults);
        }

        return ordered.ToList();
    }

    private static IReadOnlyList<Token> Tokenize(string value, CancellationToken cancellationToken)
    {
        var tokens = new List<Token>();
        int start = -1;

        for (int index = 0; index <= value.Length; index++)
        {
            if ((index & 255) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            bool isTokenCharacter = index < value.Length && char.IsLetterOrDigit(value[index]);
            if (isTokenCharacter && start < 0)
            {
                start = index;
            }
            else if (!isTokenCharacter && start >= 0)
            {
                tokens.Add(new(value[start..index], start));
                start = -1;
            }
        }

        return tokens;
    }

    private static FuzzyMatch? FindBestFuzzyMatch(
        string text,
        string query,
        CancellationToken cancellationToken)
    {
        FuzzyMatch? best = null;
        int searchStart = 0;

        while (searchStart < text.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int queryIndex = 0;
            int end = searchStart;

            for (; end < text.Length && queryIndex < query.Length; end++)
            {
                if ((end & 255) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                if (text[end] == query[queryIndex])
                {
                    queryIndex++;
                }
            }

            if (queryIndex < query.Length)
            {
                break;
            }

            int[] positions = new int[query.Length];
            queryIndex = query.Length - 1;
            for (int index = end - 1; queryIndex >= 0; index--)
            {
                if ((index & 255) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                if (text[index] == query[queryIndex])
                {
                    positions[queryIndex--] = index;
                }
            }

            int start = positions[0];
            int span = positions[^1] - start + 1;
            int gapCount = span - query.Length;
            int adjacentPairs = 0;

            for (int index = 1; index < positions.Length; index++)
            {
                if (positions[index] == positions[index - 1] + 1)
                {
                    adjacentPairs++;
                }
            }

            double adjacencyRatio = query.Length == 1
                ? 1
                : (double)adjacentPairs / (query.Length - 1);
            double compactness = (double)query.Length / span;
            double wordBoundaryBonus = start == 0 || !char.IsLetterOrDigit(text[start - 1])
                ? FuzzyWordBoundaryBonus
                : 0;
            double score = FuzzyBaseScore +
                (FuzzyAdjacencyWeight * adjacencyRatio) +
                (FuzzyCompactnessWeight * compactness) +
                wordBoundaryBonus;
            var candidate = new FuzzyMatch(score, start, gapCount);

            if (best is null || IsBetter(candidate, best.Value))
            {
                best = candidate;
            }

            searchStart = start + 1;
        }

        return best;
    }

    private static bool IsBetter(FuzzyMatch candidate, FuzzyMatch current) =>
        candidate.Score > current.Score ||
        (candidate.Score == current.Score && candidate.Start < current.Start) ||
        (candidate.Score == current.Score && candidate.Start == current.Start &&
            candidate.GapCount < current.GapCount);

    private sealed record Token(string Value, int Start);

    private readonly record struct FuzzyMatch(double Score, int Start, int GapCount);

    private sealed record RankedResult<T>(
        SearchResult<T> Result,
        int MatchStart,
        int GapCount,
        string Name,
        int SourceIndex)
        where T : INamedBusinessObject;
}
