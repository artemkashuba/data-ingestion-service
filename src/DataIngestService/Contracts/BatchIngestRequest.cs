namespace DataIngestService.Contracts;

public class BatchIngestRequest
{
    public IFormFile? File { get; init; }
}
