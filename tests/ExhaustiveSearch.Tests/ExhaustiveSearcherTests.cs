using DigitalWorxpaces.Worxpace.Base;

namespace ExhaustiveSearch.Tests;

public sealed class ExhaustiveSearcherTests
{
    [Theory]
    [InlineData("Alpha", "alpha", MatchKind.Exact, 100)]
    [InlineData("The Alpha Object", "alpha", MatchKind.ExactWord, 90)]
    [InlineData("The Alphabet Object", "alpha", MatchKind.WordPrefix, 85)]
    [InlineData("The Betalpha Object", "alpha", MatchKind.WordSuffix, 85)]
    [InlineData("The XalphaX Object", "alpha", MatchKind.Substring, 80)]
    public void FindMatches_ClassifiesDirectMatches(
        string name,
        string query,
        MatchKind expectedKind,
        double expectedScore)
    {
        INamedBusinessObject item = NamedBusinessObjectFactory.Create(name);

        SearchResult<INamedBusinessObject> result =
            Assert.Single(ExhaustiveSearcher.FindMatches([item], query));

        Assert.Same(item, result.Item);
        Assert.Equal(expectedKind, result.MatchKind);
        Assert.Equal(expectedScore, result.Score);
    }

    [Fact]
    public void FindMatches_ReturnsEmptyForEmptyOrWhitespaceQuery()
    {
        INamedBusinessObject item = NamedBusinessObjectFactory.Create("Alpha");

        Assert.Empty(ExhaustiveSearcher.FindMatches([item], string.Empty));
        Assert.Empty(ExhaustiveSearcher.FindMatches([item], " \t"));
    }

    [Fact]
    public void FindMatches_ValidatesArgumentsAndLimits()
    {
        INamedBusinessObject item = NamedBusinessObjectFactory.Create("Alpha");

        Assert.Throws<ArgumentNullException>(() =>
            ExhaustiveSearcher.FindMatches<INamedBusinessObject>(null!, "alpha"));
        Assert.Throws<ArgumentNullException>(() =>
            ExhaustiveSearcher.FindMatches([item], null!));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ExhaustiveSearcher.FindMatches(
                [item],
                "alpha",
                new SearchOptions { MaximumResults = -1 }));
        Assert.Throws<ArgumentException>(() =>
            ExhaustiveSearcher.FindMatches<INamedBusinessObject>([null!], "alpha"));
        Assert.Empty(ExhaustiveSearcher.FindMatches(
            [item],
            "alpha",
            new SearchOptions { MaximumResults = 0 }));
    }

    [Fact]
    public void FindMatches_ValidatesMinimumScore()
    {
        INamedBusinessObject item = NamedBusinessObjectFactory.Create("Alpha");

        foreach (double invalidScore in new[]
                 {
                     -1,
                     101,
                     double.NaN,
                     double.PositiveInfinity,
                     double.NegativeInfinity,
                 })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ExhaustiveSearcher.FindMatches(
                    [item],
                    "alpha",
                    new SearchOptions { MinimumScore = invalidScore }));
        }
    }

    [Fact]
    public void FindMatches_FiltersResultsBelowMinimumScore()
    {
        INamedBusinessObject exact = NamedBusinessObjectFactory.Create("Alpha");
        INamedBusinessObject substring = NamedBusinessObjectFactory.Create("XalphaX");
        INamedBusinessObject fuzzy = NamedBusinessObjectFactory.Create("A-l-p-h-a");

        IReadOnlyList<SearchResult<INamedBusinessObject>> results =
            ExhaustiveSearcher.FindMatches(
                [fuzzy, substring, exact],
                "alpha",
                new SearchOptions { MinimumScore = 85 });

        SearchResult<INamedBusinessObject> result = Assert.Single(results);
        Assert.Same(exact, result.Item);
    }

    [Fact]
    public void FindMatches_CanDisableFuzzyMatches()
    {
        INamedBusinessObject fuzzy = NamedBusinessObjectFactory.Create("A-l-p-h-a");

        Assert.Empty(ExhaustiveSearcher.FindMatches(
            [fuzzy],
            "alpha",
            new SearchOptions { IncludeFuzzyMatches = false }));
    }

    [Fact]
    public void FindMatches_ObservesCancellationBeforeEnumeration()
    {
        INamedBusinessObject item = NamedBusinessObjectFactory.Create("Alpha");
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            ExhaustiveSearcher.FindMatches(
                [item],
                "alpha",
                cancellationToken: cancellationSource.Token));
    }

    [Fact]
    public void FindMatches_AppliesMaximumResultsAndPreservesSourceOrderForTies()
    {
        INamedBusinessObject first = NamedBusinessObjectFactory.Create("Alpha First");
        INamedBusinessObject second = NamedBusinessObjectFactory.Create("Alpha Second");

        IReadOnlyList<SearchResult<INamedBusinessObject>> results =
            ExhaustiveSearcher.FindMatches(
                [first, second],
                "alpha",
                new SearchOptions { MaximumResults = 1 });

        Assert.Single(results);
        Assert.Same(first, results[0].Item);
    }

    [Fact]
    public void FindMatches_PreservesDistinctObjectsWithDuplicateNames()
    {
        INamedBusinessObject first = NamedBusinessObjectFactory.Create("Alpha");
        INamedBusinessObject second = NamedBusinessObjectFactory.Create("Alpha");

        IReadOnlyList<SearchResult<INamedBusinessObject>> results =
            ExhaustiveSearcher.FindMatches([first, second], "alpha");

        Assert.Collection(
            results,
            result => Assert.Same(first, result.Item),
            result => Assert.Same(second, result.Item));
    }

    [Fact]
    public void FindMatches_EnumeratesSourceOnce()
    {
        INamedBusinessObject item = NamedBusinessObjectFactory.Create("Alpha");
        var source = new SingleUseEnumerable<INamedBusinessObject>([item]);

        Assert.Single(ExhaustiveSearcher.FindMatches(source, "alpha"));
        Assert.Equal(1, source.EnumerationCount);
    }

    [Fact]
    public void FindMatches_RequiresEveryFuzzyQueryCharacterInOrder()
    {
        INamedBusinessObject complete = NamedBusinessObjectFactory.Create("A-b-c");
        INamedBusinessObject partial = NamedBusinessObjectFactory.Create("A lone match");
        INamedBusinessObject reversed = NamedBusinessObjectFactory.Create("C-b-a");

        SearchResult<INamedBusinessObject> result =
            Assert.Single(ExhaustiveSearcher.FindMatches([partial, reversed, complete], "abc"));

        Assert.Same(complete, result.Item);
        Assert.Equal(MatchKind.Fuzzy, result.MatchKind);
        Assert.InRange(result.Score, 0, 70);
    }

    [Fact]
    public void FindMatches_RanksCompactFuzzyMatchesAboveSpreadMatches()
    {
        INamedBusinessObject spread = NamedBusinessObjectFactory.Create("A---b---c");
        INamedBusinessObject compact = NamedBusinessObjectFactory.Create("A-b-c");

        IReadOnlyList<SearchResult<INamedBusinessObject>> results =
            ExhaustiveSearcher.FindMatches([spread, compact], "abc");

        Assert.Collection(
            results,
            result => Assert.Same(compact, result.Item),
            result => Assert.Same(spread, result.Item));
        Assert.True(results[0].Score > results[1].Score);
    }

    [Theory]
    [InlineData("A-b-c", "abcc")]
    [InlineData("A-c-b", "abc")]
    [InlineData("A-b", "abcdef")]
    public void FindMatches_RejectsIncompleteRepeatedTransposedAndLongFuzzyQueries(
        string name,
        string query)
    {
        INamedBusinessObject item = NamedBusinessObjectFactory.Create(name);

        Assert.Empty(ExhaustiveSearcher.FindMatches([item], query));
    }

    [Fact]
    public void FindMatches_MatchesUnicodeLettersAsAnOrderedSequence()
    {
        INamedBusinessObject item = NamedBusinessObjectFactory.Create("Å-b-c");

        SearchResult<INamedBusinessObject> result =
            Assert.Single(ExhaustiveSearcher.FindMatches([item], "åbc"));

        Assert.Equal(MatchKind.Fuzzy, result.MatchKind);
    }

    [Theory]
    [InlineData("Alpha-Beta", "beta", MatchKind.ExactWord)]
    [InlineData("Alpha/Beta", "bet", MatchKind.WordPrefix)]
    [InlineData("Alpha(Beta)", "eta", MatchKind.WordSuffix)]
    [InlineData("Alpha\tBeta", "beta", MatchKind.ExactWord)]
    public void FindMatches_TokenizesPunctuationAndWhitespace(
        string name,
        string query,
        MatchKind expectedKind)
    {
        INamedBusinessObject item = NamedBusinessObjectFactory.Create(name);

        SearchResult<INamedBusinessObject> result =
            Assert.Single(ExhaustiveSearcher.FindMatches([item], query));

        Assert.Equal(expectedKind, result.MatchKind);
    }

    [Fact]
    public void FindMatches_UsesNameLengthThenNameBeforeSourceOrderForOtherwiseEqualMatches()
    {
        INamedBusinessObject longest = NamedBusinessObjectFactory.Create("Alpha Charlie");
        INamedBusinessObject alphabeticallyLast = NamedBusinessObjectFactory.Create("Alpha Bravo");
        INamedBusinessObject alphabeticallyFirst = NamedBusinessObjectFactory.Create("Alpha Able");

        IReadOnlyList<SearchResult<INamedBusinessObject>> results =
            ExhaustiveSearcher.FindMatches(
                [longest, alphabeticallyLast, alphabeticallyFirst],
                "alpha");

        Assert.Collection(
            results,
            result => Assert.Same(alphabeticallyFirst, result.Item),
            result => Assert.Same(alphabeticallyLast, result.Item),
            result => Assert.Same(longest, result.Item));
    }

    private sealed class SingleUseEnumerable<T>(IEnumerable<T> source) : IEnumerable<T>
    {
        public int EnumerationCount { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            EnumerationCount++;
            if (EnumerationCount > 1)
            {
                throw new InvalidOperationException("The source was enumerated more than once.");
            }

            return source.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
