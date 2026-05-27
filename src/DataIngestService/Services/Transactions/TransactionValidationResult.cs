namespace DataIngestService.Services.Transactions;

public class TransactionValidationResult
{
    private TransactionValidationResult(IReadOnlyCollection<TransactionValidationError> errors)
    {
        Errors = errors;
    }

    public bool IsValid => Errors.Count == 0;

    public IReadOnlyCollection<TransactionValidationError> Errors { get; }

    public static TransactionValidationResult Success()
    {
        return new TransactionValidationResult(Array.Empty<TransactionValidationError>());
    }

    public static TransactionValidationResult Failure(IReadOnlyCollection<TransactionValidationError> errors)
    {
        return new TransactionValidationResult(errors);
    }
}
