using BankingLedger.Application.Common;
using BankingLedger.Application.DTOs;
using BankingLedger.Application.Interfaces;
using BankingLedger.Domain.Entities;
using BankingLedger.Domain.Exceptions;
using BankingLedger.Domain.ValueObjects;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BankingLedger.Application.Services;

public interface ITransactionService
{
    Task<TransactionDto> CreateTransactionAsync(
        Guid accountId,
        CreateTransactionRequest request,
        CancellationToken ct = default);

    Task<PagedResult<TransactionDto>> GetTransactionsAsync(
        Guid accountId,
        PaginationParams pagination,
        CancellationToken ct = default);
}

/// <summary>
/// Service coordinating financial ledger transactions.
/// 
/// Human / PM Context:
/// This is the financial engine of the ledger.
/// To safeguard money and prevent race conditions:
/// 1. We validate the input (amount > 0, valid type, reference_id).
/// 2. We check for duplicate reference_ids on the account (idempotency safeguard).
/// 3. We load the Account aggregate root.
/// 4. Within an atomic database transaction:
///    - We execute the aggregate's Credit or Debit method (which enforces balance rules).
///    - The aggregate updates its Balance AND records the Transaction atomically.
/// 5. If any step fails (e.g., balance too low, network drop, database error),
///    the transaction rolls back completely and the customer's balance is untouched.
/// </summary>
public class TransactionService : ITransactionService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateTransactionRequest> _transactionValidator;
    private readonly IValidator<PaginationParams> _paginationValidator;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateTransactionRequest> transactionValidator,
        IValidator<PaginationParams> paginationValidator,
        ILogger<TransactionService> logger)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _transactionValidator = transactionValidator;
        _paginationValidator = paginationValidator;
        _logger = logger;
    }

    public async Task<TransactionDto> CreateTransactionAsync(
        Guid accountId,
        CreateTransactionRequest request,
        CancellationToken ct = default)
    {
        // 1. Validate request payload
        var validationResult = await _transactionValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Begin explicit database transaction for full Atomicity guarantee
        await using var txScope = await _unitOfWork.BeginTransactionAsync(ct);

        // 3. Ensure the account exists
        var account = await _accountRepository.GetByIdAsync(accountId, ct);
        if (account == null)
        {
            _logger.LogWarning("Transaction rejected: Account {AccountId} not found", accountId);
            throw new AccountNotFoundException(accountId);
        }

        // 4. Duplicate reference ID check (Idempotency safeguard)
        var referenceExists = await _transactionRepository.ExistsByReferenceIdAsync(accountId, request.ReferenceId, ct);
        if (referenceExists)
        {
            _logger.LogWarning("Duplicate transaction detected for Account {AccountId} with ReferenceId '{ReferenceId}'",
                accountId, request.ReferenceId);
            throw new DuplicateReferenceException(request.ReferenceId, accountId);
        }

        // 5. Execute transaction through the Account Aggregate Root
        var money = new Money(request.Amount);
        var reference = new ReferenceId(request.ReferenceId);
        Transaction transaction;

        if (request.Type.Equals("credit", StringComparison.OrdinalIgnoreCase))
        {
            transaction = account.Credit(money, reference, request.Description);
        }
        else if (request.Type.Equals("debit", StringComparison.OrdinalIgnoreCase))
        {
            // Debit will throw InsufficientBalanceException if Balance < Amount
            transaction = account.Debit(money, reference, request.Description);
        }
        else
        {
            throw new InvalidTransactionException($"Unsupported transaction type '{request.Type}'. Allowed types: 'credit', 'debit'.");
        }

        // 6. Commit both balance change and transaction record atomically
        await _unitOfWork.SaveChangesAsync(ct);
        await txScope.CommitAsync(ct);

        _logger.LogInformation("Transaction {TransactionId} of type {Type} for amount {Amount} successfully committed to Account {AccountId}. New balance: {Balance}",
            transaction.Id, transaction.Type, transaction.Amount, account.Id, transaction.BalanceAfter);

        return MapToDto(transaction);
    }

    public async Task<PagedResult<TransactionDto>> GetTransactionsAsync(
        Guid accountId,
        PaginationParams pagination,
        CancellationToken ct = default)
    {
        // 1. Validate pagination parameters
        var validationResult = await _paginationValidator.ValidateAsync(pagination, ct);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Verify account exists
        var accountExists = await _accountRepository.ExistsAsync(accountId, ct);
        if (!accountExists)
        {
            _logger.LogWarning("Transaction listing failed: Account {AccountId} not found", accountId);
            throw new AccountNotFoundException(accountId);
        }

        // 3. Fetch paginated transactions sorted latest-first (created_at DESC)
        return await _transactionRepository.GetTransactionsByAccountIdAsync(accountId, pagination, ct);
    }

    private static TransactionDto MapToDto(Transaction transaction) => new()
    {
        Id = transaction.Id,
        AccountId = transaction.AccountId,
        Type = transaction.Type.ToString().ToLowerInvariant(),
        Amount = transaction.Amount,
        ReferenceId = transaction.ReferenceId,
        Description = transaction.Description,
        BalanceAfter = transaction.BalanceAfter,
        CreatedAt = transaction.CreatedAt
    };
}
