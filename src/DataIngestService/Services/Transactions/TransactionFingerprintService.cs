using System.Globalization;
using DataIngestService.Contracts;

namespace DataIngestService.Services.Transactions;

public class TransactionFingerprintService : ITransactionFingerprintService
{
    public string BuildDeduplicationKey(IngestTransactionRequest request)
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
