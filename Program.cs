namespace ExhaustiveSearch;
class Program
{
    public static Task<List<ResultItem>> FindMatchesAsync(List<string> list, string query, int numberOfItemsToReturn = -1, out ObservableCollection<ResultItem> output)
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

    static void Main(string[] args)
    {
        var list = new List<string>();

        string? line;
        try
        {
            StreamReader sr = new("itemsList");

            line = sr.ReadLine();

            while (line != null)
            {
                list.Add(line);
                line = sr.ReadLine();
            }
            sr.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: " + e.Message);
        }

        Console.WriteLine("Enter a query to search: ");
        string? query = Console.ReadLine();
        if (query is null) return;

        Console.WriteLine("...");
        Console.WriteLine("Query Entered: " + query);
        Console.WriteLine("Results: ");
        Console.WriteLine("");


        foreach (ResultItem item in FindMatchesAsync(list, query, 5).Result)
            Console.WriteLine(item.Item + "\t" + item.Score.ToString());
    }
}

public class ResultItem
{
    public string Item { get; set; }
    public double Score { get; set; }

    public ResultItem(string item, double score)
    {
        Item = item;
        Score = score;
    }
}
