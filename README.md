# ExhaustiveSearch

A .NET 8 class library for relevance-ranked searching over caller-supplied
`INamedBusinessObject` collections.

ExhaustiveSearch preserves each caller object in its result and classifies why the
object's non-nullable `Name` matched. It performs no filesystem, console, database,
or network I/O.

## Requirements

- .NET 8 or later
- `DigitalWorxpaces.Worxpace.Base` (referenced transitively by the package)
- Objects implementing `INamedBusinessObject`

## Installation

When the package is published, add it with the .NET CLI:

```shell
dotnet add package ExhaustiveSearch
```

For local development, reference `src/ExhaustiveSearch/ExhaustiveSearch.csproj`.

## Usage

Given a domain type such as `Customer` that implements `INamedBusinessObject`:

```csharp
using ExhaustiveSearch;

IEnumerable<Customer> customers = GetCustomers();

IReadOnlyList<SearchResult<Customer>> results = ExhaustiveSearcher.FindMatches(
    customers,
    "acme",
    new SearchOptions
    {
        MaximumResults = 10,
        MinimumScore = 80,
        IncludeFuzzyMatches = true,
    });

foreach (SearchResult<Customer> result in results)
{
    Console.WriteLine($"{result.Item.Name}: {result.MatchKind} ({result.Score:F2})");
}
```

The generic result retains the original `Customer` instance; consumers do not need
to cast back from `INamedBusinessObject`.

## Matching and scores

The first applicable direct-match rule wins:

| Match kind | Score | Meaning |
|---|---:|---|
| `Exact` | 100 | The complete name equals the query. |
| `ExactWord` | 90 | A complete letter-or-digit token equals the query. |
| `WordPrefix` | 85 | A token starts with the query. |
| `WordSuffix` | 85 | A token ends with the query. |
| `Substring` | 80 | The name contains the query. |
| `Fuzzy` | 40–70 | Every query character occurs in order in the name. |

Matching is case-insensitive. Punctuation and whitespace delimit tokens. Fuzzy
scores reward adjacent characters, compact matches, and matches beginning at a word
boundary. A fuzzy match can never outrank a direct substring match.

## Result ordering

Results are ordered by:

1. descending score;
2. earlier match position;
3. fewer fuzzy-match gaps;
4. shorter name;
5. case-insensitive name order;
6. original source position.

Distinct objects are not deduplicated, even when they have the same name.

## Input behavior

- A null collection or query throws `ArgumentNullException`.
- A null object in the collection throws `ArgumentException`.
- An empty or whitespace-only query returns no results.
- `MaximumResults = null` returns every match.
- `MaximumResults = 0` returns no results.
- A negative maximum throws `ArgumentOutOfRangeException`.
- `MinimumScore` inclusively filters the documented 0–100 score range and must be finite.
- `IncludeFuzzyMatches = false` restricts results to direct match tiers.
- An optional `CancellationToken` cancels enumeration and scoring.
- The source sequence is enumerated once and is never modified.

## Development

```shell
dotnet restore ExhaustiveSearch.sln
dotnet build ExhaustiveSearch.sln --configuration Release --no-restore
dotnet test ExhaustiveSearch.sln --configuration Release --no-build
dotnet pack src/ExhaustiveSearch/ExhaustiveSearch.csproj --configuration Release --no-build
```

The implementation plan and current status are maintained in [ROADMAP.md](ROADMAP.md).

## License

Licensed under the MIT License. See [LICENSE](LICENSE).
