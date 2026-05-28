namespace DataIngestService.Data.Entities;

public class TransactionEntity
{
    public Guid Id { get; set; }

    public required string CustomerId { get; set; }

    public string? ExternalTransactionId { get; set; }

    public DateTimeOffset TransactionDate { get; set; }

    public decimal Amount { get; set; }

    public required string Currency { get; set; }

    public required string SourceChannel { get; set; }

    public required string DeduplicationKey { get; set; }

    public DateTimeOffset IngestedAt { get; set; }
}
