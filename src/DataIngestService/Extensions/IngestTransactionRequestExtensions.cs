using DataIngestService.Contracts;
using DataIngestService.Data.Entities;

namespace DataIngestService.Extensions;

public static class IngestTransactionRequestExtensions
{
    public static TransactionEntity ToEntity(this IngestTransactionRequest request, string deduplicationKey)
    {
        return new TransactionEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId.Trim(),
            ExternalTransactionId = NormalizeOptional(request.ExternalTransactionId),
            TransactionDate = request.TransactionDate.ToUniversalTime(),
            Amount = decimal.Round(request.Amount, 2),
            Currency = request.Currency.Trim().ToUpperInvariant(),
            SourceChannel = request.SourceChannel.Trim(),
            DeduplicationKey = deduplicationKey,
            IngestedAt = DateTimeOffset.UtcNow
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }
}
