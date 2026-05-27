using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DataIngestService.Contracts;
using DataIngestService.Data;
using DataIngestService.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DataIngestService.Services.Transactions;

public class BatchTransactionIngestionService : IBatchTransactionIngestionService
{
    private const int DefaultBatchSize = 1_000;

    private readonly AppDbContext _dbContext;
    private readonly ITransactionFingerprintService _fingerprintService;
    private readonly int _batchSize;
    private readonly ITransactionValidator _validator;

    public BatchTransactionIngestionService(
        AppDbContext dbContext,
        ITransactionFingerprintService fingerprintService,
        ITransactionValidator validator,
        IOptions<IngestionOptions> options)
    {
        _dbContext = dbContext;
        _fingerprintService = fingerprintService;
        _batchSize = NormalizeBatchSize(options.Value.BatchSize);
        _validator = validator;
    }

    public async Task<BatchIngestResponse> IngestAsync(Stream csvStream, CancellationToken cancellationToken)
    {
        var totalRows = 0;
        var acceptedRows = 0;
        var duplicateRows = 0;
        var errors = new List<BatchRowError>();
        var seenDeduplicationKeys = new HashSet<string>(StringComparer.Ordinal);
        var candidates = new List<BatchTransactionCandidate>(_batchSize);

        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, CreateCsvConfiguration());

        if (!await csv.ReadAsync())
        {
            return CreateResponse(totalRows, acceptedRows, duplicateRows, errors);
        }

        csv.ReadHeader();

        var rowNumber = 1;
        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            rowNumber++;
            totalRows++;

            var rowResult = TryReadRequest(csv);
            if (!rowResult.IsValid)
            {
                errors.Add(new BatchRowError
                {
                    RowNumber = rowNumber,
                    Errors = rowResult.Errors
                });

                continue;
            }

            var validationResult = _validator.Validate(rowResult.Request!);
            if (!validationResult.IsValid)
            {
                errors.Add(new BatchRowError
                {
                    RowNumber = rowNumber,
                    Errors = validationResult.Errors.Select(error => $"{error.Field}: {error.Message}").ToArray()
                });

                continue;
            }

            var deduplicationKey = _fingerprintService.BuildDeduplicationKey(rowResult.Request!);
            if (!seenDeduplicationKeys.Add(deduplicationKey))
            {
                duplicateRows++;
                errors.Add(new BatchRowError
                {
                    RowNumber = rowNumber,
                    Errors = ["Duplicate transaction within uploaded file."]
                });

                continue;
            }

            candidates.Add(new BatchTransactionCandidate
            {
                RowNumber = rowNumber,
                Request = rowResult.Request!,
                DeduplicationKey = deduplicationKey
            });

            if (candidates.Count == _batchSize)
            {
                acceptedRows += await FlushCandidatesAsync(candidates, errors, cancellationToken);
                duplicateRows += CountDuplicateErrors(candidates, errors);
                candidates.Clear();
            }
        }

        if (candidates.Count > 0)
        {
            acceptedRows += await FlushCandidatesAsync(candidates, errors, cancellationToken);
            duplicateRows += CountDuplicateErrors(candidates, errors);
        }

        return CreateResponse(totalRows, acceptedRows, duplicateRows, errors);
    }

    private async Task<int> FlushCandidatesAsync(
        IReadOnlyCollection<BatchTransactionCandidate> candidates,
        ICollection<BatchRowError> errors,
        CancellationToken cancellationToken)
    {
        var candidateKeys = candidates
            .Select(candidate => candidate.DeduplicationKey)
            .ToArray();

        var existingKeys = await _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => candidateKeys.Contains(transaction.DeduplicationKey))
            .Select(transaction => transaction.DeduplicationKey)
            .ToListAsync(cancellationToken);

        var existingKeySet = existingKeys.ToHashSet(StringComparer.Ordinal);
        var acceptedEntities = candidates
            .Where(candidate =>
            {
                if (!existingKeySet.Contains(candidate.DeduplicationKey))
                {
                    return true;
                }

                errors.Add(new BatchRowError
                {
                    RowNumber = candidate.RowNumber,
                    Errors = ["Duplicate transaction already exists."]
                });

                return false;
            })
            .Select(candidate => candidate.Request.ToEntity(candidate.DeduplicationKey))
            .ToArray();

        if (acceptedEntities.Length == 0)
        {
            return 0;
        }

        _dbContext.Transactions.AddRange(acceptedEntities);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _dbContext.ChangeTracker.Clear();

        return acceptedEntities.Length;
    }

    private static BatchIngestResponse CreateResponse(
        int totalRows,
        int acceptedRows,
        int duplicateRows,
        IReadOnlyCollection<BatchRowError> errors)
    {
        return new BatchIngestResponse
        {
            TotalRows = totalRows,
            AcceptedRows = acceptedRows,
            RejectedRows = errors.Count,
            DuplicateRows = duplicateRows,
            Errors = errors.ToArray()
        };
    }

    private static int CountDuplicateErrors(
        IEnumerable<BatchTransactionCandidate> candidates,
        IEnumerable<BatchRowError> errors)
    {
        var candidateRows = candidates
            .Select(candidate => candidate.RowNumber)
            .ToHashSet();

        return errors.Count(error =>
            candidateRows.Contains(error.RowNumber)
            && error.Errors.Any(message => message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)));
    }

    private static BatchRowReadResult TryReadRequest(CsvReader csv)
    {
        var errors = new List<string>();

        var customerId = GetField(csv, CsvTransactionHeaders.CustomerId);
        var externalTransactionId = GetField(csv, CsvTransactionHeaders.ExternalTransactionId);
        var transactionDateValue = GetField(csv, CsvTransactionHeaders.TransactionDate);
        var amountValue = GetField(csv, CsvTransactionHeaders.Amount);
        var currency = GetField(csv, CsvTransactionHeaders.Currency);
        var sourceChannel = GetField(csv, CsvTransactionHeaders.SourceChannel);

        if (!DateTimeOffset.TryParse(
                transactionDateValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var transactionDate))
        {
            errors.Add("TransactionDate: Value must be a valid date/time.");
        }

        if (!decimal.TryParse(amountValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            errors.Add("Amount: Value must be a valid decimal number.");
        }

        if (errors.Count > 0)
        {
            return BatchRowReadResult.Invalid(errors);
        }

        return BatchRowReadResult.Valid(new IngestTransactionRequest
        {
            CustomerId = customerId ?? string.Empty,
            ExternalTransactionId = externalTransactionId,
            TransactionDate = transactionDate,
            Amount = amount,
            Currency = currency ?? string.Empty,
            SourceChannel = sourceChannel ?? string.Empty
        });
    }

    private static string? GetField(CsvReader csv, IEnumerable<string> headerAliases)
    {
        foreach (var headerAlias in headerAliases)
        {
            if (csv.TryGetField<string>(headerAlias, out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static CsvConfiguration CreateCsvConfiguration()
    {
        return new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            BadDataFound = null,
            HeaderValidated = null,
            MissingFieldFound = null,
            PrepareHeaderForMatch = args => NormalizeHeader(args.Header),
            TrimOptions = TrimOptions.Trim
        };
    }

    private static string NormalizeHeader(string header)
    {
        return header.Trim().Replace(" ", string.Empty).ToLowerInvariant();
    }

    private static int NormalizeBatchSize(int batchSize)
    {
        return batchSize > 0 ? batchSize : DefaultBatchSize;
    }
}
