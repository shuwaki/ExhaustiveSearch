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
