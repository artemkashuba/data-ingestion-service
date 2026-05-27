using DataIngestService.Contracts;

namespace DataIngestService.Services.Transactions;

public class BatchTransactionCandidate
{
    public required int RowNumber { get; init; }

    public required IngestTransactionRequest Request { get; init; }

    public required string DeduplicationKey { get; init; }
}
