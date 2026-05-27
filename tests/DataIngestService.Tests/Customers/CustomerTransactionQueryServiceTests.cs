using DataIngestService.Contracts;
using DataIngestService.Data;
using DataIngestService.Data.Entities;
using DataIngestService.Services.Customers;
using Microsoft.EntityFrameworkCore;

namespace DataIngestService.Tests.Customers;

public class CustomerTransactionQueryServiceTests
{
    [Fact]
    public async Task GetTransactionsAsync_ReturnsPagedCustomerTransactionsNewestFirst()
    {
        await using var dbContext = CreateDbContext();
        SeedTransactions(dbContext);
        var service = new CustomerTransactionQueryService(dbContext);

        var response = await service.GetTransactionsAsync(
            "customer-1",
            new CustomerTransactionsQuery
            {
                Page = 1,
                PageSize = 2
            },
            CancellationToken.None);

        Assert.Equal(2, response.Items.Count);
        Assert.Equal(1, response.Page);
        Assert.Equal(2, response.PageSize);
        Assert.Equal(3, response.TotalCount);
        Assert.Equal(2, response.TotalPages);
        Assert.Collection(
            response.Items,
            item => Assert.Equal(30m, item.Amount),
            item => Assert.Equal(20m, item.Amount));
    }

    [Fact]
    public async Task GetTransactionsAsync_AppliesDateCurrencyAndSourceChannelFilters()
    {
        await using var dbContext = CreateDbContext();
        SeedTransactions(dbContext);
        var service = new CustomerTransactionQueryService(dbContext);

        var response = await service.GetTransactionsAsync(
            "customer-1",
            new CustomerTransactionsQuery
            {
                From = new DateTimeOffset(2026, 5, 27, 10, 30, 0, TimeSpan.Zero),
                To = new DateTimeOffset(2026, 5, 27, 11, 30, 0, TimeSpan.Zero),
                Currency = "usd",
                SourceChannel = "WEB"
            },
            CancellationToken.None);

        var item = Assert.Single(response.Items);
        Assert.Equal(20m, item.Amount);
        Assert.Equal("USD", item.Currency);
        Assert.Equal("web", item.SourceChannel);
    }

    [Fact]
    public async Task GetTransactionsAsync_NormalizesInvalidPaginationValues()
    {
        await using var dbContext = CreateDbContext();
        SeedTransactions(dbContext);
        var service = new CustomerTransactionQueryService(dbContext);

        var response = await service.GetTransactionsAsync(
            "customer-1",
            new CustomerTransactionsQuery
            {
                Page = -1,
                PageSize = 500
            },
            CancellationToken.None);

        Assert.Equal(1, response.Page);
        Assert.Equal(200, response.PageSize);
        Assert.Equal(3, response.TotalCount);
        Assert.Equal(1, response.TotalPages);
    }

    private static void SeedTransactions(AppDbContext dbContext)
    {
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
                amount: 20m,
                currency: "USD",
                sourceChannel: "web"),
            CreateTransaction(
                customerId: "customer-1",
                transactionDate: new DateTimeOffset(2026, 5, 27, 12, 0, 0, TimeSpan.Zero),
                amount: 30m,
                currency: "EUR",
                sourceChannel: "email"),
            CreateTransaction(
                customerId: "customer-2",
                transactionDate: new DateTimeOffset(2026, 5, 27, 13, 0, 0, TimeSpan.Zero),
                amount: 40m,
                currency: "USD",
                sourceChannel: "web"));

        dbContext.SaveChanges();
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
            IngestedAt = new DateTimeOffset(2026, 5, 27, 14, 0, 0, TimeSpan.Zero)
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
