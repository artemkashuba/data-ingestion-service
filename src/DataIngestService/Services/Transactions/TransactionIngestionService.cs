using DataIngestService.Contracts;
using DataIngestService.Data;
using DataIngestService.Extensions;
using Microsoft.EntityFrameworkCore;

namespace DataIngestService.Services.Transactions;

public class TransactionIngestionService : ITransactionIngestionService
{
    private readonly AppDbContext _dbContext;
    private readonly ITransactionFingerprintService _fingerprintService;
    private readonly ITransactionValidator _validator;

    public TransactionIngestionService(
        AppDbContext dbContext,
        ITransactionFingerprintService fingerprintService,
        ITransactionValidator validator)
    {
        _dbContext = dbContext;
        _fingerprintService = fingerprintService;
        _validator = validator;
    }

    public async Task<TransactionIngestionResult> IngestAsync(
        IngestTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            return TransactionIngestionResult.ValidationFailed(validationResult.Errors);
        }

        var deduplicationKey = _fingerprintService.BuildDeduplicationKey(request);
        var duplicateExists = await _dbContext.Transactions
            .AsNoTracking()
            .AnyAsync(transaction => transaction.DeduplicationKey == deduplicationKey, cancellationToken);

        if (duplicateExists)
        {
            return TransactionIngestionResult.Duplicate();
        }

        var transaction = request.ToEntity(deduplicationKey);
        _dbContext.Transactions.Add(transaction);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            return TransactionIngestionResult.Duplicate();
        }

        return TransactionIngestionResult.Created(transaction);
    }
}
