namespace DataIngestService.Contracts;

public class CustomerTransactionsQuery
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 50;

    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }

    public string? Currency { get; init; }

    public string? SourceChannel { get; init; }
}
