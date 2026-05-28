using DataIngestService.Contracts;
using DataIngestService.Services.Transactions;

namespace DataIngestService.Tests.Transactions;

public class TransactionValidatorTests
{
    private readonly FixedTimeProvider _timeProvider = new(new DateTimeOffset(2026, 5, 27, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Validate_ReturnsSuccess_ForValidRequest()
    {
        var validator = new TransactionValidator(_timeProvider);

        var result = validator.Validate(CreateRequest());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ReturnsErrors_ForInvalidBusinessFields()
    {
        var validator = new TransactionValidator(_timeProvider);
        var request = CreateRequest(
            customerId: " ",
            transactionDate: _timeProvider.GetUtcNow().AddMinutes(6),
            amount: 0,
            currency: "US1",
            sourceChannel: " ");

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Field == nameof(IngestTransactionRequest.CustomerId));
        Assert.Contains(result.Errors, error => error.Field == nameof(IngestTransactionRequest.TransactionDate));
        Assert.Contains(result.Errors, error => error.Field == nameof(IngestTransactionRequest.Amount));
        Assert.Contains(result.Errors, error => error.Field == nameof(IngestTransactionRequest.Currency));
        Assert.Contains(result.Errors, error => error.Field == nameof(IngestTransactionRequest.SourceChannel));
    }

    [Fact]
    public void Validate_AllowsSmallFutureClockSkew()
    {
        var validator = new TransactionValidator(_timeProvider);
        var request = CreateRequest(transactionDate: _timeProvider.GetUtcNow().AddMinutes(5));

        var result = validator.Validate(request);

        Assert.True(result.IsValid);
    }

    private static IngestTransactionRequest CreateRequest(
        string customerId = "customer-1",
        DateTimeOffset? transactionDate = null,
        decimal amount = 19.99m,
        string currency = "USD",
        string sourceChannel = "web")
    {
        return new IngestTransactionRequest
        {
            CustomerId = customerId,
            TransactionDate = transactionDate ?? new DateTimeOffset(2026, 5, 27, 10, 0, 0, TimeSpan.Zero),
            Amount = amount,
            Currency = currency,
            SourceChannel = sourceChannel
        };
    }

    private class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}
