using BankingLedger.Application.DTOs;
using BankingLedger.Application.Interfaces;
using BankingLedger.Domain.Entities;
using BankingLedger.Domain.Exceptions;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BankingLedger.Application.Services;

public interface IAccountService
{
    Task<AccountDto> CreateAccountAsync(CreateAccountRequest request, CancellationToken ct = default);
    Task<AccountDto> GetAccountByIdAsync(Guid id, CancellationToken ct = default);
    Task<AccountBalanceDto> GetAccountBalanceAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AccountDto>> GetAllAccountsAsync(CancellationToken ct = default);
}

/// <summary>
/// Service coordinating Account business operations.
/// 
/// Human / PM Context:
/// When an account is created, we use the Domain factory method (Account.Create) to enforce rules.
/// If an initial balance was specified, the aggregate root automatically records an opening Credit transaction.
/// We then persist the aggregate via the Unit of Work.
/// </summary>
public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateAccountRequest> _validator;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateAccountRequest> validator,
        ILogger<AccountService> logger)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<AccountDto> CreateAccountAsync(CreateAccountRequest request, CancellationToken ct = default)
    {
        // 1. Validate payload using FluentValidation
        var validationResult = await _validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Instantiate Aggregate Root (domain encapsulates all invariants)
        var initialBalance = request.InitialBalance ?? 0m;
        var account = Account.Create(request.AccountName, initialBalance);

        // 3. Persist atomically
        await _accountRepository.AddAsync(account, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Account {AccountId} successfully opened for '{AccountName}' with initial balance {Balance}",
            account.Id, account.Name, account.Balance);

        return MapToDto(account);
    }

    public async Task<AccountDto> GetAccountByIdAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _accountRepository.GetByIdAsync(id, ct);
        if (account == null)
        {
            _logger.LogWarning("Account query failed: Account {AccountId} not found", id);
            throw new AccountNotFoundException(id);
        }

        return MapToDto(account);
    }

    public async Task<AccountBalanceDto> GetAccountBalanceAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _accountRepository.GetByIdAsync(id, ct);
        if (account == null)
        {
            _logger.LogWarning("Balance inquiry failed: Account {AccountId} not found", id);
            throw new AccountNotFoundException(id);
        }

        return new AccountBalanceDto
        {
            AccountId = account.Id,
            Balance = account.Balance,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    public async Task<IReadOnlyList<AccountDto>> GetAllAccountsAsync(CancellationToken ct = default)
    {
        var accounts = await _accountRepository.GetAllAsync(ct);
        return accounts.Select(MapToDto).ToList();
    }

    private static AccountDto MapToDto(Account account) => new()
    {
        Id = account.Id,
        Name = account.Name,
        Balance = account.Balance,
        CreatedAt = account.CreatedAt,
        UpdatedAt = account.UpdatedAt
    };
}
