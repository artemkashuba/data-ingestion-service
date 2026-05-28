using DataIngestService.Contracts;
using DataIngestService.Data;
using Microsoft.EntityFrameworkCore;

namespace DataIngestService.Services.Stats;

public class StatsSummaryService : IStatsSummaryService
{
    private readonly AppDbContext _dbContext;

    public StatsSummaryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StatsSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var transactions = _dbContext.Transactions.AsNoTracking();
        var totalTransactions = await transactions.CountAsync(cancellationToken);

        if (totalTransactions == 0)
        {
            return new StatsSummaryResponse();
        }

        var totalUniqueCustomers = await transactions
            .Select(transaction => transaction.CustomerId)
            .Distinct()
            .CountAsync(cancellationToken);

        var totalAmount = await transactions.SumAsync(transaction => transaction.Amount, cancellationToken);

        var breakdownByCurrency = await transactions
            .GroupBy(transaction => transaction.Currency)
            .Select(group => new StatsBreakdownItem
            {
                Key = group.Key,
                Count = group.Count(),
                TotalAmount = group.Sum(transaction => transaction.Amount)
            })
            .OrderBy(item => item.Key)
            .ToListAsync(cancellationToken);

        var breakdownBySourceChannel = await transactions
            .GroupBy(transaction => transaction.SourceChannel)
            .Select(group => new StatsBreakdownItem
            {
                Key = group.Key,
                Count = group.Count(),
                TotalAmount = group.Sum(transaction => transaction.Amount)
            })
            .OrderBy(item => item.Key)
            .ToListAsync(cancellationToken);

        var minTransactionDate = await transactions.MinAsync(transaction => transaction.TransactionDate, cancellationToken);
        var maxTransactionDate = await transactions.MaxAsync(transaction => transaction.TransactionDate, cancellationToken);

        return new StatsSummaryResponse
        {
            TotalTransactions = totalTransactions,
            TotalUniqueCustomers = totalUniqueCustomers,
            TotalAmount = totalAmount,
            BreakdownByCurrency = breakdownByCurrency,
            BreakdownBySourceChannel = breakdownBySourceChannel,
            MinTransactionDate = minTransactionDate,
            MaxTransactionDate = maxTransactionDate
        };
    }
}
