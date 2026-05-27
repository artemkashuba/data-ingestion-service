using DataIngestService.Data;
using DataIngestService.Data.Entities;
using DataIngestService.Services.Stats;
using Microsoft.EntityFrameworkCore;

namespace DataIngestService.Tests.Stats;

public class StatsSummaryServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_ReturnsAggregateSummary()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Transactions.AddRange(
            CreateTransaction(
                customerId: "customer-1",
                transactionDate: new DateTimeOffset(2026, 5, 27, 10, 0, 0, TimeSpan.Zero),
                amount: 10m,
                currency: "USD",
                sourceChannel: "web"),
            CreateTransaction(
                customerId: "customer-1",
                transactionDate: new DateTimeOffset(2026, 5, 27, 11, 0, 0, TimeSpan.Zero),
                amount: 15m,
                currency: "USD",
                sourceChannel: "email"),
            CreateTransaction(
                customerId: "customer-2",
                transactionDate: new DateTimeOffset(2026, 5, 27, 12, 0, 0, TimeSpan.Zero),
                amount: 20m,
                currency: "EUR",
                sourceChannel: "web"));
        await dbContext.SaveChangesAsync();

        var service = new StatsSummaryService(dbContext);

        var response = await service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(3, response.TotalTransactions);
        Assert.Equal(2, response.TotalUniqueCustomers);
        Assert.Equal(45m, response.TotalAmount);
        Assert.Equal(new DateTimeOffset(2026, 5, 27, 10, 0, 0, TimeSpan.Zero), response.MinTransactionDate);
        Assert.Equal(new DateTimeOffset(2026, 5, 27, 12, 0, 0, TimeSpan.Zero), response.MaxTransactionDate);

        Assert.Collection(
            response.BreakdownByCurrency,
            item =>
            {
                Assert.Equal("EUR", item.Key);
                Assert.Equal(1, item.Count);
                Assert.Equal(20m, item.TotalAmount);
            },
            item =>
            {
                Assert.Equal("USD", item.Key);
                Assert.Equal(2, item.Count);
                Assert.Equal(25m, item.TotalAmount);
            });

        Assert.Collection(
            response.BreakdownBySourceChannel,
            item =>
            {
                Assert.Equal("email", item.Key);
                Assert.Equal(1, item.Count);
                Assert.Equal(15m, item.TotalAmount);
            },
            item =>
            {
                Assert.Equal("web", item.Key);
                Assert.Equal(2, item.Count);
                Assert.Equal(30m, item.TotalAmount);
            });
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsEmptySummary_WhenNoTransactionsExist()
    {
        await using var dbContext = CreateDbContext();
        var service = new StatsSummaryService(dbContext);

        var response = await service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(0, response.TotalTransactions);
        Assert.Equal(0, response.TotalUniqueCustomers);
        Assert.Equal(0m, response.TotalAmount);
        Assert.Empty(response.BreakdownByCurrency);
        Assert.Empty(response.BreakdownBySourceChannel);
        Assert.Null(response.MinTransactionDate);
        Assert.Null(response.MaxTransactionDate);
    }

    private static TransactionEntity CreateTransaction(
        string customerId,
        DateTimeOffset transactionDate,
        decimal amount,
        string currency,
        string sourceChannel)
    {
        return new TransactionEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            TransactionDate = transactionDate,
            Amount = amount,
            Currency = currency,
            SourceChannel = sourceChannel,
            DeduplicationKey = Guid.NewGuid().ToString(),
            IngestedAt = new DateTimeOffset(2026, 5, 27, 13, 0, 0, TimeSpan.Zero)
        };
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
