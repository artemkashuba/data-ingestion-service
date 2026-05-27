using DataIngestService.Contracts;

namespace DataIngestService.Services.Transactions;

public interface ITransactionFingerprintService
{
    string BuildDeduplicationKey(IngestTransactionRequest request);
}
