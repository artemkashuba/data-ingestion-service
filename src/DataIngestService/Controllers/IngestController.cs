using DataIngestService.Contracts;
using DataIngestService.Data;
using DataIngestService.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataIngestService.Controllers;

[ApiController]
[Route("ingest")]
public class IngestController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public IngestController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("transaction")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TransactionResponse>> IngestTransaction(
        IngestTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var transaction = request.ToEntity();

        _dbContext.Transactions.Add(transaction);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            return Conflict(new ProblemDetails
            {
                Title = "Duplicate transaction",
                Detail = "A transaction with the same deduplication key already exists.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var response = transaction.ToResponse();
        return CreatedAtAction(nameof(GetTransaction), new { id = response.Id }, response);
    }

    [HttpGet("transaction/{id:guid}")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> GetTransaction(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await _dbContext.Transactions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (transaction is null)
        {
            return NotFound();
        }

        return Ok(transaction.ToResponse());
    }
}
