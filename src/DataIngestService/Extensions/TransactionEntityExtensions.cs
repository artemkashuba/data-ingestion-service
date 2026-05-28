using DataIngestService.Contracts;
using DataIngestService.Data.Entities;

namespace DataIngestService.Extensions;

public static class TransactionEntityExtensions
{
    public static TransactionResponse ToResponse(this TransactionEntity transaction)
    {
        return new TransactionResponse
        {
            Id = transaction.Id,
            CustomerId = transaction.CustomerId,
            ExternalTransactionId = transaction.ExternalTransactionId,
            TransactionDate = transaction.TransactionDate,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            SourceChannel = transaction.SourceChannel,
            IngestedAt = transaction.IngestedAt
        };
    }
}
