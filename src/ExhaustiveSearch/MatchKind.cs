namespace ExhaustiveSearch;

/// <summary>
/// Describes how a business object's name matched a search query.
/// </summary>
public enum MatchKind
{
    /// <summary>The complete name equals the query.</summary>
    Exact,

    /// <summary>A complete letter-or-digit token equals the query.</summary>
    ExactWord,

    /// <summary>A letter-or-digit token starts with the query.</summary>
    WordPrefix,

    /// <summary>A letter-or-digit token ends with the query.</summary>
    WordSuffix,

    /// <summary>The name contains the query.</summary>
    Substring,

    /// <summary>The name contains every query character in order.</summary>
    Fuzzy,
}
