namespace DataIngestService.Contracts;

public class BatchRowError
{
    public int RowNumber { get; init; }

    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();
}
