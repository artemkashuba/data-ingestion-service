using DataIngestService.Contracts;
using DataIngestService.Services.Stats;
using Microsoft.AspNetCore.Mvc;

namespace DataIngestService.Controllers;

[ApiController]
[Route("stats")]
public class StatsController : ControllerBase
{
    private readonly IStatsSummaryService _statsSummaryService;

    public StatsController(IStatsSummaryService statsSummaryService)
    {
        _statsSummaryService = statsSummaryService;
    }

    [HttpGet("summary")]
    [ProducesResponseType<StatsSummaryResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StatsSummaryResponse>> GetSummary(CancellationToken cancellationToken)
    {
        var response = await _statsSummaryService.GetSummaryAsync(cancellationToken);

        return Ok(response);
    }
}
