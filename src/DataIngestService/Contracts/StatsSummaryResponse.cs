namespace DataIngestService.Contracts;

public class StatsSummaryResponse
{
    public int TotalTransactions { get; init; }

    public int TotalUniqueCustomers { get; init; }

    public decimal TotalAmount { get; init; }

    public IReadOnlyCollection<StatsBreakdownItem> BreakdownByCurrency { get; init; } = Array.Empty<StatsBreakdownItem>();

    public IReadOnlyCollection<StatsBreakdownItem> BreakdownBySourceChannel { get; init; } = Array.Empty<StatsBreakdownItem>();

    public DateTimeOffset? MinTransactionDate { get; init; }

    public DateTimeOffset? MaxTransactionDate { get; init; }
}
