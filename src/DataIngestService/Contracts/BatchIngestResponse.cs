namespace DataIngestService.Contracts;

public class BatchIngestResponse
{
    public int TotalRows { get; init; }

    public int AcceptedRows { get; init; }

    public int RejectedRows { get; init; }

    public int DuplicateRows { get; init; }

    public IReadOnlyCollection<BatchRowError> Errors { get; init; } = Array.Empty<BatchRowError>();
}
