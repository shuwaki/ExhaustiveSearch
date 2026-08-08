# ExhaustiveSearch remediation roadmap

The first milestone is a breaking architectural conversion:

> Convert the console executable into a reusable .NET 8 class library, reference
> `DigitalWorxpaces.Worxpace.Base`, and search caller-supplied collections of
> `INamedBusinessObject`.

`INamedBusinessObject.Name` is non-nullable. The exact package version and namespace
must be confirmed when package restore is available.

## Progress snapshot

Status markers used below are **Complete**, **In progress**, and **Planned**.

- **Complete — Change set 1:** the project is now a .NET 8 library; the package
  reference and generic `INamedBusinessObject` API are present; immutable search
  results, match kinds, and options have been introduced; and console/file-based
  application artifacts have been removed.
- **Complete — Change set 2:** a .NET 8 xUnit project records direct behavior,
  validation, limits, identity, ordering, and single-enumeration semantics.
- **In progress — build verification:** the dependency version and namespace are now
  confirmed, but the current environment has no .NET SDK. Execution of the test and
  pack pipeline remains delegated to CI or an equipped development environment.
- **Complete — Change set 3:** fuzzy matching now requires a complete ordered
  subsequence, rewards compact and adjacent matches, supports punctuation-aware
  tokenization, and applies deterministic secondary ordering.
- **Complete — Change set 4a:** source and test projects now use a conventional
  `src`/`tests` layout, generated artifacts and IDE state are no longer tracked,
  the ignore rules cover common .NET tooling, and the static API has been renamed
  from controller terminology to `ExhaustiveSearcher`.
- **Complete — Change set 4b:** the README now documents the library API and search
  contract, the license uses a conventional filename, NuGet metadata and Source Link
  are configured, and CI restores, builds, tests, packs, and uploads artifacts.
- **Complete — API completion:** `SearchOptions` supports result limits, minimum-score
  filtering, and fuzzy-match inclusion, while the synchronous search API supports
  cooperative cancellation.
- **Next — release verification:** run the CI restore/build/test/pack pipeline and
  address any compiler, package-contract, analyzer, or test feedback it produces.

## Milestone 1 — Convert the application into a .NET 8 library

### 1. Change the SDK project to a class library — Complete

- Change `TargetFramework` from `net6.0` to `net8.0`.
- Remove `<OutputType>Exe</OutputType>` or set it to `Library`.
- Keep nullable reference types and implicit usings enabled.
- Add appropriate package metadata and XML documentation generation.
- Verify that `dotnet build` emits a DLL and `dotnet pack` creates a valid package.

### 2. Add `DigitalWorxpaces.Worxpace.Base` — Complete

- Add a `PackageReference` with an explicit stable version.
- Verify its `net8.0` compatibility, namespace, and transitive dependencies.
- Use the package's actual `INamedBusinessObject`; do not duplicate it locally.

Confirmed contract: package version `1.0.7` exposes `INamedBusinessObject` from the
`DigitalWorxpaces.Worxpace.Base` namespace, and `Name` is non-nullable.

### 3. Replace string inputs with `INamedBusinessObject` — Complete

Design the primary API around caller-owned objects, preferably preserving their
concrete type:

```csharp
IReadOnlyList<SearchResult<T>> FindMatches<T>(
    IEnumerable<T> items,
    string query,
    SearchOptions? options = null)
    where T : INamedBusinessObject;
```

- The caller supplies the collection.
- The result retains the original object instance and concrete type.
- Accept a collection abstraction rather than `List<T>`.

### 4. Replace `ResultItem` with a typed, immutable search result — Complete

Use an immutable result in the library namespace, for example:

```csharp
public sealed record SearchResult<T>(
    T Item,
    double Score,
    MatchKind MatchKind)
    where T : INamedBusinessObject;
```

The score meaning and match classification must be documented.

### 5. Remove the console application entry point from the library — Complete

- Remove `Program.cs` from the library.
- Keep all filesystem and `Console` operations out of the package.
- If useful, add a separate sample console project that references the library.

### 6. Remove the built-in medication corpus from the library — Complete

- Remove the embedded `itemsList` resource configuration.
- Move the data to a sample or test fixture if it remains useful, or remove it.
- Make search behavior independent of the filesystem and working directory.

## Milestone 2 — Establish a clean public API contract

### 7. Make the search API synchronous — Complete

Rename `FindMatchesAsync` to `FindMatches` or `Search`, return results directly,
and remove `Task.FromResult` and `.Result`. An in-memory CPU loop should not expose
artificial asynchronous behavior.

### 8. Decide whether the service is static or injectable — Complete

Use either a clearly named static utility such as `ExhaustiveSearcher`, or an
`IExhaustiveSearcher` service if dependency injection, mocking, shared
configuration, or interchangeable algorithms are required. Do not call it a
controller unless it serves that role.

Decision: retain a stateless static API named `ExhaustiveSearcher`. The current
algorithm has no injected dependencies or shared mutable configuration.

### 9. Introduce a search-options type — Complete

Replace the magic `-1` limit with documented options, for example:

```csharp
public sealed record SearchOptions
{
    public int? MaximumResults { get; init; }
    public double MinimumScore { get; init; }
    public StringComparison Comparison { get; init; }
}
```

Potential later options include fuzzy matching, tokenization, tie-breaking,
whitespace handling, and empty-query behavior.

### 10. Define argument validation — Complete

| Condition | Recommended behavior |
|---|---|
| `items` is null | `ArgumentNullException` |
| `query` is null | `ArgumentNullException` |
| Query is empty/whitespace | Return an empty result set |
| Item is null | Reject or skip, but document it |
| Maximum results is negative | `ArgumentOutOfRangeException` |
| Maximum results is zero | Return an empty result set |

`INamedBusinessObject.Name` is non-nullable, so the implementation may rely on
that interface contract while still deciding whether defensive runtime checks are
valuable for incorrectly implemented objects.

### 11. Define enumeration semantics — Complete

- Enumerate the source exactly once.
- Buffer only matches.
- Preserve original source position as the final stable tie-breaker.
- Do not modify the caller's collection.
- Document that infinite sequences are not supported.

## Milestone 3 — Correct and formalize search behavior

### 12. Search the `INamedBusinessObject` name consistently — Complete

- Extract `Name` once per object.
- Decide whether surrounding whitespace is significant.
- Use one comparison/normalization strategy consistently.
- Prefer `StringComparison.OrdinalIgnoreCase` by default.
- Never mutate the original object or its name.

### 13. Clarify empty-query behavior — Complete

Return an empty result set for an empty or whitespace-only query so it cannot
accidentally match the entire collection.

### 14. Replace literal-space tokenization — Complete

Define tokenization for whitespace, hyphens, punctuation, slashes, parentheses,
and repeated separators. Add tests for each supported boundary type.

### 15. Replace or correct the fuzzy matcher — Complete

Choose and document the intended definition:

- **Full subsequence:** require all query characters in order and penalize gaps,
  late starts, and long names.
- **Edit distance:** support substitutions, omissions, and transpositions.
- **Hybrid:** retain exact/token/prefix/substring tiers, followed by a
  gap-penalized subsequence or edit-distance score.

A hybrid is closest to the project's current intent. Full fuzzy matches should
rank above partial matches, adjacent characters above widely separated matches,
and scores must remain within a documented range.

### 16. Implement the promised proximity bonus — Complete

Either implement adjacency and gap-aware scoring or remove the claim. Possible
components are base coverage, adjacency and word-start bonuses, and gap,
late-start, and unmatched-query penalties.

### 17. Make scoring semantics explicit — Complete

Introduce a documented `MatchKind`, such as `Exact`, `ExactToken`, `TokenPrefix`,
`TokenSuffix`, `Substring`, and `Fuzzy`. Use named score constants or a dedicated
scorer, protect tier boundaries with tests, and ensure fuzzy scores cannot outrank
stronger categories unless explicitly intended.

### 18. Add deterministic tie-breaking — Complete

Order by descending score, then intentional secondary criteria such as match
start, gap count, name length, name, and finally original source position.

### 19. Decide duplicate-object and duplicate-name behavior — Complete

Do not deduplicate by default. Distinct business objects can legitimately share a
name. Any future deduplication must be opt-in and use the domain identity contract.

### 20. Add optional cancellation only if warranted — Complete

If collections may be large, accept an optional `CancellationToken` in the
synchronous method. Do not invent an async API solely to support cancellation.

## Milestone 4 — Test coverage

### 21. Create a .NET 8 test project — Complete

Add a separate test project under a conventional structure such as:

```text
src/ExhaustiveSearch/
tests/ExhaustiveSearch.Tests/
samples/ExhaustiveSearch.Sample/
```

Use small test implementations of `INamedBusinessObject` and ensure `dotnet test`
discovers and runs the suite.

### 22. Add API-validation tests — Complete

Cover null collection/query, empty and whitespace query, null item, zero and
negative maximums, empty collections, and single-use enumerables.

### 23. Add relevance tests — Complete

Cover exact name, exact token, token prefix, token suffix, substring, fuzzy, and
no-match behavior, including the ordering between tiers.

### 24. Add fuzzy edge-case tests — Complete

Cover contiguous and gapped sequences, missing characters, repeated characters,
reversed characters, transpositions, long queries, punctuation, and Unicode. If
full Unicode correctness is required, evaluate `Rune` rather than UTF-16 `char`.

### 25. Add ordering and identity tests — Complete

Cover equal scores, duplicate names on different objects, repeated references,
source-order stability, concrete generic type preservation, and result immutability.

### 26. Add package/build checks — Complete

Automate restore, Release build, tests, packing, formatting, and analyzers. Treat
warnings as errors after the initial migration is clean.

## Milestone 5 — Repository cleanup

### 27. Replace the incomplete `.gitignore` — Complete

Adopt a standard .NET/Visual Studio ignore file covering `bin`, `obj`, `.vs`, test
results, coverage, user-specific IDE files, and package output.

### 28. Remove committed build outputs and IDE state — Complete

Remove tracked `bin`, `obj`, `.vs`, and user-specific workspace files. Keep editor
configuration only when it is useful to every contributor.

### 29. Remove or implement `IDrug` — Complete

Remove the empty `IDrug` artifact. The library should rely on the package's
`INamedBusinessObject` rather than introduce an unused medication-specific type.

### 30. Normalize namespaces and style — Complete

- Put all public types in one deliberate library namespace.
- Use a consistent namespace style.
- Remove unused usings.
- Add XML documentation to public APIs.
- Apply standard .NET formatting and analyzers.

## Milestone 6 — Documentation and packaging

### 31. Rewrite the README around library usage — Complete

Document the .NET 8 requirement, package installation, dependency on
`DigitalWorxpaces.Worxpace.Base`, `INamedBusinessObject` input, a basic search
example, result and scoring semantics, edge-case behavior, and performance.

### 32. Correct the project description — Complete

Describe the implemented match kinds rather than claiming that any shared single
character necessarily constitutes a match.

### 33. Repair README links and remove template residue — Complete

Remove the missing screenshot and `example.com` target, replace circular links
with real documentation, remove unused framework references, and repair navigation.

### 34. Correct license naming and metadata — Complete

Verify provenance and attribution, rename the license to a conventional filename,
make README and package metadata agree with it, and use the `MIT` package license
expression if legally correct.

The filename, README link, package expression, and repository-owner attribution now
agree. The unrelated attribution inherited from the README template has been
removed.

### 35. Add release/package metadata — Complete

Add a semantic-versioning policy, package description, repository link, Source
Link and symbols if desired, XML documentation, changelog, release notes, and a
supported-framework statement.

## Suggested implementation sequence

### Change set 1 — architecture conversion

1. Convert to a .NET 8 library.
2. Add the package reference.
3. Replace string input with generic `INamedBusinessObject` input.
4. Introduce immutable `SearchResult<T>`.
5. Remove `Program.cs` and the bundled corpus.
6. Preserve current ranking behavior during the mechanical migration.

### Change set 2 — tests around inherited behavior

1. Add the test project.
2. Encode current exact/token/prefix/substring behavior.
3. Add tests exposing known fuzzy defects.
4. Define validation and empty-query semantics.

### Change set 3 — scoring correction

1. Introduce `MatchKind`.
2. Implement the chosen fuzzy algorithm.
3. Add proximity/gap scoring.
4. Add deterministic tie-breaking.
5. Document score semantics.

### Change set 4 — cleanup and packaging

1. Clean `.gitignore` and tracked artifacts.
2. Normalize namespaces and style.
3. Rewrite the README.
4. Correct license references.
5. Add package metadata and CI.

## Confirmed implementation decisions

1. Use generic `SearchResult<T>` so callers receive their concrete type.
2. Search the non-nullable `Name` declared by `INamedBusinessObject`.
3. Pin the latest compatible stable package version available from the configured
   NuGet source.
4. Return an empty result set for an empty or whitespace-only query.
5. Reject a null collection or query; define defensive behavior for null items.
6. Use a synchronous generic search method and immutable options record.
7. Remove the current executable initially and add a separate sample only after
   the library and tests compile.
