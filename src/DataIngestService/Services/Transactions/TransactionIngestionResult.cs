using DataIngestService.Data.Entities;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DataIngestService.Services.Transactions;

public class TransactionIngestionResult
{
    private TransactionIngestionResult(
        TransactionIngestionStatus status,
        TransactionEntity? transaction,
        IReadOnlyCollection<TransactionValidationError> validationErrors)
    {
        Status = status;
        Transaction = transaction;
        ValidationErrors = validationErrors;
    }

    public TransactionIngestionStatus Status { get; }

    public TransactionEntity? Transaction { get; }

    public IReadOnlyCollection<TransactionValidationError> ValidationErrors { get; }

    public bool IsCreated => Status == TransactionIngestionStatus.Created;

    public bool IsDuplicate => Status == TransactionIngestionStatus.Duplicate;

    public bool IsValidationFailed => Status == TransactionIngestionStatus.ValidationFailed;

    public static TransactionIngestionResult Created(TransactionEntity transaction)
    {
        return new TransactionIngestionResult(
            TransactionIngestionStatus.Created,
            transaction,
            validationErrors: []);
    }

    public static TransactionIngestionResult Duplicate()
    {
        return new TransactionIngestionResult(
            TransactionIngestionStatus.Duplicate,
            transaction: null,
            validationErrors: []);
    }

    public static TransactionIngestionResult ValidationFailed(IReadOnlyCollection<TransactionValidationError> errors)
    {
        return new TransactionIngestionResult(TransactionIngestionStatus.ValidationFailed, null, errors);
    }

    public ModelStateDictionary ToModelStateDictionary()
    {
        var modelState = new ModelStateDictionary();

        foreach (var error in ValidationErrors)
        {
            modelState.AddModelError(error.Field, error.Message);
        }

        return modelState;
    }
}
