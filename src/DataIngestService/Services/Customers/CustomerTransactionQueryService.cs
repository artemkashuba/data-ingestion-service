using DataIngestService.Contracts;
using DataIngestService.Data;
using DataIngestService.Data.Entities;
using DataIngestService.Extensions;
using Microsoft.EntityFrameworkCore;

namespace DataIngestService.Services.Customers;

public class CustomerTransactionQueryService : ICustomerTransactionQueryService
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;

    private readonly AppDbContext _dbContext;

    public CustomerTransactionQueryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResponse<TransactionResponse>> GetTransactionsAsync(
        string customerId,
        CustomerTransactionsQuery query,
        CancellationToken cancellationToken)
    {
        var page = NormalizePage(query.Page);
        var pageSize = NormalizePageSize(query.PageSize);
        var transactionsQuery = ApplyFilters(
            _dbContext.Transactions.AsNoTracking(),
            customerId,
            query);

        var totalCount = await transactionsQuery.CountAsync(cancellationToken);
        var transactions = await transactionsQuery
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<TransactionResponse>
        {
            Items = transactions.Select(transaction => transaction.ToResponse()).ToArray(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = CalculateTotalPages(totalCount, pageSize)
        };
    }

    private static IQueryable<TransactionEntity> ApplyFilters(
        IQueryable<TransactionEntity> query,
        string customerId,
        CustomerTransactionsQuery filters)
    {
        var normalizedCustomerId = customerId.Trim();
        query = query.Where(transaction => transaction.CustomerId == normalizedCustomerId);

        if (filters.From is not null)
        {
            var from = filters.From.Value.ToUniversalTime();
            query = query.Where(transaction => transaction.TransactionDate >= from);
        }

        if (filters.To is not null)
        {
            var to = filters.To.Value.ToUniversalTime();
            query = query.Where(transaction => transaction.TransactionDate <= to);
        }

        if (!string.IsNullOrWhiteSpace(filters.Currency))
        {
            var currency = filters.Currency.Trim().ToUpperInvariant();
            query = query.Where(transaction => transaction.Currency == currency);
        }

        if (!string.IsNullOrWhiteSpace(filters.SourceChannel))
        {
            var sourceChannel = filters.SourceChannel.Trim().ToUpperInvariant();
            query = query.Where(transaction => transaction.SourceChannel.ToUpper() == sourceChannel);
        }

        return query;
    }

    private static int NormalizePage(int page)
    {
        return page > 0 ? page : DefaultPage;
    }

    private static int NormalizePageSize(int pageSize)
    {
        if (pageSize <= 0)
        {
            return DefaultPageSize;
        }

        return Math.Min(pageSize, MaxPageSize);
    }

    private static int CalculateTotalPages(int totalCount, int pageSize)
    {
        return totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
    }
}
