using DataIngestService.Contracts;

namespace DataIngestService.Services.Customers;

public interface ICustomerTransactionQueryService
{
    Task<PagedResponse<TransactionResponse>> GetTransactionsAsync(
        string customerId,
        CustomerTransactionsQuery query,
        CancellationToken cancellationToken);
}
