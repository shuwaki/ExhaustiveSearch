using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExhaustiveSearch
{
    public static class SearchController
    {
        public static Task<List<ResultItem>> FindMatchesAsync(List<string> list, string query, int numberOfItemsToReturn = -1)
        {
            var results = new List<ResultItem>();
            foreach (string item in list)
            {
                if (item.ToLower() == query.ToLower())
                    results.Add(new(item, 100));
                else
                {
                    var itemParts = item.Split();
                    if (itemParts.Any(x => x.ToLower() == query.ToLower()))
                        results.Add(new(item, 90));
                    else if (itemParts.Any(x => x.StartsWith(query, StringComparison.OrdinalIgnoreCase) || x.EndsWith(query, StringComparison.OrdinalIgnoreCase)))
                        results.Add(new(item, 85));
                    else if (item.Contains(query, StringComparison.OrdinalIgnoreCase) || query.Contains(item, StringComparison.OrdinalIgnoreCase))
                        results.Add(new(item, 80));
                    else
                    {
                        var chars = query.ToCharArray();
                        var unitScore = (1d / chars.Length) * 70;
                        ResultItem resultItem = new(item, 0);

                        for (int i = 0; i < chars.Length; i++)
                        {
                            if (item.Contains(chars[i], StringComparison.OrdinalIgnoreCase))
                            {
                                resultItem.Item = item;
                                resultItem.Score += unitScore;
                                chars.SetValue(null, i);
                            }
                        }
                        if (resultItem.Score > 0)
                            results.Add(resultItem);
                    }


                }

            }
            if (numberOfItemsToReturn == -1)
                return Task.FromResult(results.OrderByDescending(x => x.Score).ToList());
            else
                return Task.FromResult(results.OrderByDescending(x => x.Score).Take(numberOfItemsToReturn).ToList());
        }

    }
}
