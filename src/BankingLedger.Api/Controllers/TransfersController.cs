using BankingLedger.Application.DTOs;
using BankingLedger.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankingLedger.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class TransfersController : ControllerBase
{
    private readonly ITransferService _transferService;

    public TransfersController(ITransferService transferService)
    {
        _transferService = transferService;
    }

    /// <summary>
    /// Transfer money atomically between two accounts.
    /// 
    /// Financial Rules Enforced:
    /// 1. Both source and destination accounts must exist.
    /// 2. Source account must have sufficient funds.
    /// 3. Cannot transfer to the same account.
    /// 4. Atomicity: The debit from Account A and credit to Account B occur in one database transaction.
    ///    A partial transfer is mathematically impossible.
    /// 5. Idempotency: Duplicate transfer reference_id is rejected.
    /// </summary>
    /// <param name="request">Transfer instructions (source account, destination account, amount, reference_id).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transfer confirmation with updated balances for both accounts.</returns>
    /// <response code="200">Transfer completed successfully.</response>
    /// <response code="400">Invalid transfer parameters or self-transfer attempt.</response>
    /// <response code="404">Source or destination account not found.</response>
    /// <response code="409">Duplicate reference_id for transfer.</response>
    /// <response code="422">Source account has insufficient balance.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TransferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> TransferMoney(
        [FromBody] TransferRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _transferService.TransferMoneyAsync(request, cancellationToken);
        return Ok(result);
    }
}
