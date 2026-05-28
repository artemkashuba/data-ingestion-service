namespace DataIngestService.Services.Transactions;

public class IngestionOptions
{
    public int BatchSize { get; init; } = 1_000;
}
