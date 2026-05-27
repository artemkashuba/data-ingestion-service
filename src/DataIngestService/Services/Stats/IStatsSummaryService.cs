using DataIngestService.Contracts;

namespace DataIngestService.Services.Stats;

public interface IStatsSummaryService
{
    Task<StatsSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken);
}
