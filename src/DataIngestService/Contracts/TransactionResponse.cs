namespace DataIngestService.Contracts;

public class TransactionResponse
{
    public Guid Id { get; init; }

    public required string CustomerId { get; init; }

    public string? ExternalTransactionId { get; init; }

    public DateTimeOffset TransactionDate { get; init; }

    public decimal Amount { get; init; }

    public required string Currency { get; init; }

    public required string SourceChannel { get; init; }

    public DateTimeOffset IngestedAt { get; init; }
}
