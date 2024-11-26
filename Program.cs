namespace ExhaustiveSearch;
class Program
{
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


        foreach (ResultItem item in SearchController.FindMatchesAsync(list, query, 5).Result)
            Console.WriteLine(item.Item + "\t" + item.Score.ToString());
    }
}

