using DataIngestService.Contracts;

namespace DataIngestService.Services.Transactions;

public interface ITransactionValidator
{
    TransactionValidationResult Validate(IngestTransactionRequest request);
}
