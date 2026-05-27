using DataIngestService.Contracts;
using DataIngestService.Data.Entities;

namespace DataIngestService.Services.Transactions;

public interface ITransactionIngestionService
{
    Task<TransactionIngestionResult> IngestAsync(IngestTransactionRequest request, CancellationToken cancellationToken);

    Task<TransactionEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
