namespace DataIngestService.Services.Transactions;

public record TransactionValidationError(string Field, string Message);
