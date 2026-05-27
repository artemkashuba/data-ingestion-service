namespace DataIngestService.Services.Transactions;

public class IngestionOptions
{
    public const string SectionName = "Ingestion";

    public int BatchSize { get; init; } = 1_000;
}
