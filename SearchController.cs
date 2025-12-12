using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExhaustiveSearch
{
public static class SearchController
{
    public static Task<List<ResultItem>> FindMatchesAsync(
        List<string> list,
        string query,
        int numberOfItemsToReturn = -1)
    {
        string q = query.ToLowerInvariant();
        int qLen = q.Length;

        var results = new List<ResultItem>(capacity: 128);

        foreach (string original in list)
        {
            string item = original.ToLowerInvariant();

            // --- Scoring rules ---
            if (item == q)
            {
                results.Add(new(original, 100));
                continue;
            }

            // split once only if needed
            string[] parts = null;

            // word equals
            if ((parts ??= item.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Any(p => p == q))
            {
                results.Add(new(original, 90));
                continue;
            }

            // word starts/ends with
            if (parts.Any(p =>
                    p.StartsWith(q, StringComparison.OrdinalIgnoreCase) ||
                    p.EndsWith(q, StringComparison.OrdinalIgnoreCase)))
            {
                results.Add(new(original, 85));
                continue;
            }

            // contains substring
            if (item.Contains(q, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new(original, 80));
                continue;
            }

            // --- Order-aware character sequence scoring ---
            double score = ScoreSequentialMatch(item, q);

            if (score > 0)
                results.Add(new(original, score));
        }

        var ordered = results.OrderByDescending(x => x.Score);

        if (numberOfItemsToReturn == -1)
            return Task.FromResult(ordered.ToList());

        return Task.FromResult(ordered.Take(numberOfItemsToReturn).ToList());
    }


    // NEW: Order-aware fuzzy scoring
    private static double ScoreSequentialMatch(string text, string query)
    {
        int ti = 0;
        int qi = 0;
        int matches = 0;

        while (ti < text.Length && qi < query.Length)
        {
            if (text[ti] == query[qi])
            {
                matches++;
                qi++;
            }
            ti++;
        }

        if (matches == 0)
            return 0;

        double ratio = (double)matches / query.Length;

        // sequential bonus: matches that stay together score much higher
        double score = 70 * ratio;

        return score;
    }
}
}