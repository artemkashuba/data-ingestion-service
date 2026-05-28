using DataIngestService.Contracts;

namespace DataIngestService.Services.Transactions;

public interface ITransactionIngestionService
{
    Task<TransactionIngestionResult> IngestAsync(IngestTransactionRequest request, CancellationToken cancellationToken);
}
