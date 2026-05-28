using DataIngestService.Contracts;

namespace DataIngestService.Services.Transactions;

public class TransactionValidator : ITransactionValidator
{
    private const int MaxCustomerIdLength = 128;
    private const int MaxExternalTransactionIdLength = 128;
    private const int MaxSourceChannelLength = 64;
    private static readonly TimeSpan FutureClockSkew = TimeSpan.FromMinutes(5);

    private readonly TimeProvider _timeProvider;

    public TransactionValidator(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public TransactionValidationResult Validate(IngestTransactionRequest request)
    {
        var errors = new List<TransactionValidationError>();

        ValidateRequiredText(errors, nameof(request.CustomerId), request.CustomerId, MaxCustomerIdLength);
        ValidateOptionalText(errors, nameof(request.ExternalTransactionId), request.ExternalTransactionId, MaxExternalTransactionIdLength);
        ValidateTransactionDate(errors, request.TransactionDate);
        ValidateAmount(errors, request.Amount);
        ValidateCurrency(errors, request.Currency);
        ValidateRequiredText(errors, nameof(request.SourceChannel), request.SourceChannel, MaxSourceChannelLength);

        return errors.Count == 0
            ? TransactionValidationResult.Success()
            : TransactionValidationResult.Failure(errors);
    }

    private static void ValidateRequiredText(
        ICollection<TransactionValidationError> errors,
        string field,
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new TransactionValidationError(field, "Value is required."));
            return;
        }

        if (value.Trim().Length > maxLength)
        {
            errors.Add(new TransactionValidationError(field, $"Value must be {maxLength} characters or fewer."));
        }
    }

    private static void ValidateOptionalText(
        ICollection<TransactionValidationError> errors,
        string field,
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (value.Trim().Length > maxLength)
        {
            errors.Add(new TransactionValidationError(field, $"Value must be {maxLength} characters or fewer."));
        }
    }

    private void ValidateTransactionDate(ICollection<TransactionValidationError> errors, DateTimeOffset transactionDate)
    {
        if (transactionDate == default)
        {
            errors.Add(new TransactionValidationError(nameof(IngestTransactionRequest.TransactionDate), "Value is required."));
            return;
        }

        var latestAcceptedDate = _timeProvider.GetUtcNow().Add(FutureClockSkew);
        if (transactionDate.ToUniversalTime() > latestAcceptedDate)
        {
            errors.Add(new TransactionValidationError(
                nameof(IngestTransactionRequest.TransactionDate),
                "Value cannot be more than 5 minutes in the future."));
        }
    }

    private static void ValidateAmount(ICollection<TransactionValidationError> errors, decimal amount)
    {
        if (amount <= 0)
        {
            errors.Add(new TransactionValidationError(nameof(IngestTransactionRequest.Amount), "Value must be greater than zero."));
        }
    }

    private static void ValidateCurrency(ICollection<TransactionValidationError> errors, string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            errors.Add(new TransactionValidationError(nameof(IngestTransactionRequest.Currency), "Value is required."));
            return;
        }

        var normalizedCurrency = currency.Trim();
        if (normalizedCurrency.Length != 3 || normalizedCurrency.Any(character => !char.IsAsciiLetter(character)))
        {
            errors.Add(new TransactionValidationError(nameof(IngestTransactionRequest.Currency), "Value must be a 3-letter currency code."));
        }
    }
}
