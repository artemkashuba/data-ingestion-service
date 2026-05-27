namespace DataIngestService.Contracts;

public class StatsBreakdownItem
{
    public required string Key { get; init; }

    public int Count { get; init; }

    public decimal TotalAmount { get; init; }
}
