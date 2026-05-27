using DataIngestService.Contracts;
using DataIngestService.Extensions;
using DataIngestService.Services.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace DataIngestService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngestController : ControllerBase
{
    private readonly IBatchTransactionIngestionService _batchTransactionIngestionService;
    private readonly ITransactionIngestionService _transactionIngestionService;

    public IngestController(
        ITransactionIngestionService transactionIngestionService,
        IBatchTransactionIngestionService batchTransactionIngestionService)
    {
        _transactionIngestionService = transactionIngestionService;
        _batchTransactionIngestionService = batchTransactionIngestionService;
    }

    [HttpPost("batch")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<BatchIngestResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BatchIngestResponse>> IngestBatch(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Empty file",
                Detail = "The uploaded CSV file is empty.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        await using var stream = file.OpenReadStream();
        var response = await _batchTransactionIngestionService.IngestAsync(stream, cancellationToken);

        return Ok(response);
    }

    [HttpPost("transaction")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TransactionResponse>> IngestTransaction(
        IngestTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _transactionIngestionService.IngestAsync(request, cancellationToken);

        if (result.IsValidationFailed)
        {
            return BadRequest(new ValidationProblemDetails(result.ToModelStateDictionary())
            {
                Title = "Transaction validation failed",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (result.IsDuplicate)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Duplicate transaction",
                Detail = "A transaction with the same deduplication key already exists.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var response = result.Transaction!.ToResponse();
        return CreatedAtAction(nameof(GetTransaction), new { id = response.Id }, response);
    }

    [HttpGet("transaction/{id:guid}")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> GetTransaction(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await _transactionIngestionService.GetByIdAsync(id, cancellationToken);

        if (transaction is null)
        {
            return NotFound();
        }

        return Ok(transaction.ToResponse());
    }
}
