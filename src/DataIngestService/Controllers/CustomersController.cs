using DataIngestService.Contracts;
using DataIngestService.Services.Customers;
using Microsoft.AspNetCore.Mvc;

namespace DataIngestService.Controllers;

[ApiController]
[Route("customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerTransactionQueryService _customerTransactionQueryService;

    public CustomersController(ICustomerTransactionQueryService customerTransactionQueryService)
    {
        _customerTransactionQueryService = customerTransactionQueryService;
    }

    [HttpGet("{customerId}/transactions")]
    [ProducesResponseType<PagedResponse<TransactionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<TransactionResponse>>> GetTransactions(
        string customerId,
        [FromQuery] CustomerTransactionsQuery query,
        CancellationToken cancellationToken)
    {
        var response = await _customerTransactionQueryService.GetTransactionsAsync(
            customerId,
            query,
            cancellationToken);

        return Ok(response);
    }
}
