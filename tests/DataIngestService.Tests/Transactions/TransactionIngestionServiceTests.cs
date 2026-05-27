using DataIngestService.Contracts;
using DataIngestService.Data;
using DataIngestService.Services.Transactions;
using Microsoft.EntityFrameworkCore;

namespace DataIngestService.Tests.Transactions;

public class TransactionIngestionServiceTests
{
    private readonly FixedTimeProvider _timeProvider = new(new DateTimeOffset(2026, 5, 27, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task IngestAsync_CreatesTransaction_WhenRequestIsValidAndUnique()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.IngestAsync(CreateRequest(), CancellationToken.None);

        Assert.True(result.IsCreated);
        Assert.NotNull(result.Transaction);
        Assert.Equal("customer-1", result.Transaction.CustomerId);
        Assert.Equal("USD", result.Transaction.Currency);
        Assert.Equal("composite|CUSTOMER-1|2026-05-27T10:00:00.0000000+00:00|19.99|USD|WEB", result.Transaction.DeduplicationKey);
        Assert.Equal(1, await dbContext.Transactions.CountAsync());
    }

    [Fact]
    public async Task IngestAsync_ReturnsDuplicate_WhenDeduplicationKeyAlreadyExists()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var request = CreateRequest();

        var firstResult = await service.IngestAsync(request, CancellationToken.None);
        var secondResult = await service.IngestAsync(request, CancellationToken.None);

        Assert.True(firstResult.IsCreated);
        Assert.True(secondResult.IsDuplicate);
        Assert.Equal(1, await dbContext.Transactions.CountAsync());
    }

    [Fact]
    public async Task IngestAsync_ReturnsValidationFailed_WhenRequestIsInvalid()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        var request = CreateRequest(amount: -1);

        var result = await service.IngestAsync(request, CancellationToken.None);

        Assert.True(result.IsValidationFailed);
        Assert.Contains(result.ValidationErrors, error => error.Field == nameof(IngestTransactionRequest.Amount));
        Assert.Equal(0, await dbContext.Transactions.CountAsync());
    }

    private TransactionIngestionService CreateService(AppDbContext dbContext)
    {
        return new TransactionIngestionService(
            dbContext,
            new TransactionFingerprintService(),
            new TransactionValidator(_timeProvider));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static IngestTransactionRequest CreateRequest(decimal amount = 19.99m)
    {
        return new IngestTransactionRequest
        {
            CustomerId = " customer-1 ",
            TransactionDate = new DateTimeOffset(2026, 5, 27, 10, 0, 0, TimeSpan.Zero),
            Amount = amount,
            Currency = " usd ",
            SourceChannel = " Web "
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
