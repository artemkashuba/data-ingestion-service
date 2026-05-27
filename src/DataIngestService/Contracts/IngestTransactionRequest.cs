using System.ComponentModel.DataAnnotations;

namespace DataIngestService.Contracts;

public class IngestTransactionRequest
{
    [Required]
    [MaxLength(128)]
    public required string CustomerId { get; init; }

    [MaxLength(128)]
    public string? ExternalTransactionId { get; init; }

    [Required]
    public DateTimeOffset TransactionDate { get; init; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal Amount { get; init; }

    [Required]
    [RegularExpression("^[A-Za-z]{3}$")]
    public required string Currency { get; init; }

    [Required]
    [MaxLength(64)]
    public required string SourceChannel { get; init; }
}
