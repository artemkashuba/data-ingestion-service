using DataIngestService.Contracts;

namespace DataIngestService.Services.Transactions;

public interface IBatchTransactionIngestionService
{
    Task<BatchIngestResponse> IngestAsync(Stream csvStream, CancellationToken cancellationToken);
}
