using DataIngestService.Contracts;

namespace DataIngestService.Services.Transactions;

public class BatchRowReadResult
{
    private BatchRowReadResult(IngestTransactionRequest? request, IReadOnlyCollection<string> errors)
    {
        Request = request;
        Errors = errors;
    }

    public bool IsValid => Errors.Count == 0;

    public IngestTransactionRequest? Request { get; }

    public IReadOnlyCollection<string> Errors { get; }

    public static BatchRowReadResult Valid(IngestTransactionRequest request)
    {
        return new BatchRowReadResult(request, errors: []);
    }

    public static BatchRowReadResult Invalid(IReadOnlyCollection<string> errors)
    {
        return new BatchRowReadResult(null, errors);
    }
}
