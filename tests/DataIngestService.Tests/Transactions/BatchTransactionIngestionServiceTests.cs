using System.Text;
using DataIngestService.Data;
using DataIngestService.Data.Entities;
using DataIngestService.Services.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DataIngestService.Tests.Transactions;

public class BatchTransactionIngestionServiceTests
{
    private readonly FixedTimeProvider _timeProvider = new(new DateTimeOffset(2026, 5, 27, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task IngestAsync_AcceptsValidRowsAndReportsInvalidAndDuplicateRows()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        await using var stream = CreateCsvStream("""
            customerId,transactionDate,amount,currency,sourceChannel,externalTransactionId
            customer-1,2026-05-27T10:00:00Z,19.99,USD,web,
            customer-1,2026-05-27T10:00:00Z,19.99,USD,web,
            customer-2,2026-05-27T10:00:00Z,-1,USD,web,
            customer-3,not-a-date,10,USD,web,
            """);

        var response = await service.IngestAsync(stream, CancellationToken.None);

        Assert.Equal(4, response.TotalRows);
        Assert.Equal(1, response.AcceptedRows);
        Assert.Equal(3, response.RejectedRows);
        Assert.Equal(1, response.DuplicateRows);
        Assert.Equal(3, response.Errors.Count);
        Assert.Contains(response.Errors, error => error.RowNumber == 3 && error.Errors.Contains("Duplicate transaction within uploaded file."));
        Assert.Contains(response.Errors, error => error.RowNumber == 4 && error.Errors.Any(message => message.StartsWith("Amount:", StringComparison.Ordinal)));
        Assert.Contains(response.Errors, error => error.RowNumber == 5 && error.Errors.Any(message => message.StartsWith("TransactionDate:", StringComparison.Ordinal)));
        Assert.Equal(1, await dbContext.Transactions.CountAsync());
    }

    [Fact]
    public async Task IngestAsync_ReportsDuplicate_WhenTransactionAlreadyExists()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Transactions.Add(new TransactionEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = "customer-1",
            TransactionDate = new DateTimeOffset(2026, 5, 27, 10, 0, 0, TimeSpan.Zero),
            Amount = 19.99m,
            Currency = "USD",
            SourceChannel = "web",
            DeduplicationKey = "composite|CUSTOMER-1|2026-05-27T10:00:00.0000000+00:00|19.99|USD|WEB",
            IngestedAt = _timeProvider.GetUtcNow()
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        await using var stream = CreateCsvStream("""
            customerId,transactionDate,amount,currency,sourceChannel
            customer-1,2026-05-27T10:00:00Z,19.99,USD,web
            """);

        var response = await service.IngestAsync(stream, CancellationToken.None);

        Assert.Equal(1, response.TotalRows);
        Assert.Equal(0, response.AcceptedRows);
        Assert.Equal(1, response.RejectedRows);
        Assert.Equal(1, response.DuplicateRows);
        Assert.Contains(response.Errors, error => error.RowNumber == 2 && error.Errors.Contains("Duplicate transaction already exists."));
        Assert.Equal(1, await dbContext.Transactions.CountAsync());
    }

    [Fact]
    public async Task IngestAsync_SupportsAssignmentStyleHeaderAliases()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);
        await using var stream = CreateCsvStream("""
            customer identifier,transaction date,amount,currency,source channel
            customer-1,2026-05-27T10:00:00Z,19.99,USD,web
            """);

        var response = await service.IngestAsync(stream, CancellationToken.None);

        Assert.Equal(1, response.TotalRows);
        Assert.Equal(1, response.AcceptedRows);
        Assert.Empty(response.Errors);
    }

    [Fact]
    public async Task IngestAsync_UsesConfiguredBatchSize()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, batchSize: 1);
        await using var stream = CreateCsvStream("""
            customerId,transactionDate,amount,currency,sourceChannel
            customer-1,2026-05-27T10:00:00Z,19.99,USD,web
            customer-2,2026-05-27T10:01:00Z,29.99,USD,web
            """);

        var response = await service.IngestAsync(stream, CancellationToken.None);

        Assert.Equal(2, response.TotalRows);
        Assert.Equal(2, response.AcceptedRows);
        Assert.Equal(2, await dbContext.Transactions.CountAsync());
    }

    private BatchTransactionIngestionService CreateService(AppDbContext dbContext, int batchSize = 1_000)
    {
        return new BatchTransactionIngestionService(
            dbContext,
            new TransactionFingerprintService(),
            new TransactionValidator(_timeProvider),
            Options.Create(new IngestionOptions
            {
                BatchSize = batchSize
            }));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static MemoryStream CreateCsvStream(string csv)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(csv));
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
