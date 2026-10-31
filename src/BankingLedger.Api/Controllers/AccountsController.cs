using BankingLedger.Application.DTOs;
using BankingLedger.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankingLedger.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    /// <summary>
    /// Open a new bank account in the ledger.
    /// </summary>
    /// <param name="request">Account creation details including name and optional initial deposit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created Account with unique AccountId.</returns>
    /// <response code="201">Account successfully opened.</response>
    /// <response code="400">Account name missing or invalid initial balance.</response>
    [HttpPost]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAccount(
        [FromBody] CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var account = await _accountService.CreateAccountAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAccountById), new { id = account.Id }, account);
    }

    /// <summary>
    /// Retrieve full details of an existing account.
    /// </summary>
    /// <param name="id">Unique UUID of the account.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Account details and current ledger balance.</returns>
    /// <response code="200">Account found.</response>
    /// <response code="404">Account not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountById(Guid id, CancellationToken cancellationToken)
    {
        var account = await _accountService.GetAccountByIdAsync(id, cancellationToken);
        return Ok(account);
    }

    /// <summary>
    /// Retrieve the current balance for an account.
    /// </summary>
    /// <param name="id">Unique UUID of the account.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Account balance with timestamp.</returns>
    /// <response code="200">Current balance returned.</response>
    /// <response code="404">Account not found.</response>
    [HttpGet("{id:guid}/balance")]
    [ProducesResponseType(typeof(AccountBalanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountBalance(Guid id, CancellationToken cancellationToken)
    {
        var balance = await _accountService.GetAccountBalanceAsync(id, cancellationToken);
        return Ok(balance);
    }

    /// <summary>
    /// List all registered accounts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of accounts.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AccountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAccounts(CancellationToken cancellationToken)
    {
        var accounts = await _accountService.GetAllAccountsAsync(cancellationToken);
        return Ok(accounts);
    }
}
