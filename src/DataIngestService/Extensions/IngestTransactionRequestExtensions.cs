using System.Globalization;
using DataIngestService.Contracts;
using DataIngestService.Data.Entities;

namespace DataIngestService.Extensions;

public static class IngestTransactionRequestExtensions
{
    public static TransactionEntity ToEntity(this IngestTransactionRequest request)
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
            DeduplicationKey = request.BuildDeduplicationKey(),
            IngestedAt = DateTimeOffset.UtcNow
        };
    }

    public static string BuildDeduplicationKey(this IngestTransactionRequest request)
    {
        var sourceChannel = NormalizeRequired(request.SourceChannel);
        var externalTransactionId = NormalizeOptional(request.ExternalTransactionId);

        if (externalTransactionId is not null)
        {
            return string.Join('|', "external", sourceChannel, externalTransactionId);
        }

        return string.Join(
            '|',
            "composite",
            NormalizeRequired(request.CustomerId),
            request.TransactionDate.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            decimal.Round(request.Amount, 2).ToString("0.00", CultureInfo.InvariantCulture),
            NormalizeRequired(request.Currency),
            sourceChannel);
    }

    private static string NormalizeRequired(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : NormalizeRequired(value);
    }
}
