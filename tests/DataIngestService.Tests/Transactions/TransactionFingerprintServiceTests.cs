using DataIngestService.Contracts;
using DataIngestService.Services.Transactions;

namespace DataIngestService.Tests.Transactions;

public class TransactionFingerprintServiceTests
{
    private readonly TransactionFingerprintService _service = new();

    [Fact]
    public void BuildDeduplicationKey_UsesExternalTransactionId_WhenPresent()
    {
        var request = CreateRequest(
            sourceChannel: " Web ",
            externalTransactionId: " txn-123 ");

        var key = _service.BuildDeduplicationKey(request);

        Assert.Equal("external|WEB|TXN-123", key);
    }

    [Fact]
    public void BuildDeduplicationKey_UsesNormalizedCompositeFields_WhenExternalTransactionIdIsMissing()
    {
        var request = CreateRequest(
            customerId: " customer-1 ",
            transactionDate: new DateTimeOffset(2026, 5, 27, 13, 15, 0, TimeSpan.FromHours(3)),
            amount: 19.999m,
            currency: " usd ",
            sourceChannel: " Web ");

        var key = _service.BuildDeduplicationKey(request);

        Assert.Equal("composite|CUSTOMER-1|2026-05-27T10:15:00.0000000+00:00|20.00|USD|WEB", key);
    }

    private static IngestTransactionRequest CreateRequest(
        string customerId = "customer-1",
        string? externalTransactionId = null,
        DateTimeOffset? transactionDate = null,
        decimal amount = 19.99m,
        string currency = "USD",
        string sourceChannel = "web")
    {
        return new IngestTransactionRequest
        {
            CustomerId = customerId,
            ExternalTransactionId = externalTransactionId,
            TransactionDate = transactionDate ?? new DateTimeOffset(2026, 5, 27, 10, 15, 0, TimeSpan.Zero),
            Amount = amount,
            Currency = currency,
            SourceChannel = sourceChannel
        };
    }
}
