using BankingLedger.Application.Common;
using BankingLedger.Application.DTOs;
using BankingLedger.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankingLedger.Api.Controllers;

[ApiController]
[Route("api/v1/accounts/{accountId:guid}/[controller]")]
[Produces("application/json")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    /// <summary>
    /// Post a new credit or debit transaction against an account.
    /// 
    /// Financial Rules Enforced:
    /// 1. Transaction Immutability: Once created, transactions cannot be edited or deleted.
    /// 2. Atomicity: Both the transaction record and the account balance update are committed as one atomic database unit.
    /// 3. Idempotency: Duplicate reference_id for the same account is rejected with 409 Conflict.
    /// 4. Overdraft Protection: Debit transactions check that the account has sufficient balance; otherwise 422 Unprocessable Entity.
    /// </summary>
    /// <param name="accountId">Unique UUID of the account.</param>
    /// <param name="request">Transaction details (type, amount, reference_id, description).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created immutable transaction record.</returns>
    /// <response code="201">Transaction successfully created and balance updated.</response>
    /// <response code="400">Invalid transaction parameters or malformed input.</response>
    /// <response code="404">Account does not exist.</response>
    /// <response code="409">Duplicate reference_id for this account.</response>
    /// <response code="422">Debit rejected due to insufficient account balance.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateTransaction(
        [FromRoute] Guid accountId,
        [FromBody] CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var transaction = await _transactionService.CreateTransactionAsync(accountId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, transaction);
    }

    /// <summary>
    /// Retrieve paginated transaction history for an account.
    /// 
    /// Features:
    /// - Sorting: Transactions are always returned in latest-first order based on created_at.
    /// - Pagination: Supported via page and limit parameters.
    /// - Metadata: Returns current page, page size, total records, and total pages.
    /// </summary>
    /// <param name="accountId">Unique UUID of the account.</param>
    /// <param name="page">Page number (1-based, default 1).</param>
    /// <param name="limit">Number of records per page (default 10, max 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated result containing transaction history and page metadata.</returns>
    /// <response code="200">Transactions retrieved successfully.</response>
    /// <response code="400">Invalid pagination parameters.</response>
    /// <response code="404">Account not found.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactions(
        [FromRoute] Guid accountId,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var pagination = new PaginationParams { Page = page, Limit = limit };
        var result = await _transactionService.GetTransactionsAsync(accountId, pagination, cancellationToken);
        return Ok(result);
    }
}
