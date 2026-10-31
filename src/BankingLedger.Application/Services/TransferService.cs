using BankingLedger.Application.DTOs;
using BankingLedger.Application.Interfaces;
using BankingLedger.Domain.Entities;
using BankingLedger.Domain.Exceptions;
using BankingLedger.Domain.ValueObjects;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BankingLedger.Application.Services;

public interface ITransferService
{
    Task<TransferDto> TransferMoneyAsync(TransferRequest request, CancellationToken ct = default);
}

/// <summary>
/// Service coordinating inter-account atomic money transfers.
/// 
/// Human / PM Context:
/// Money transfer is a classic two-legged financial transaction:
/// - Leg 1: Debit Account A (sender)
/// - Leg 2: Credit Account B (recipient)
/// 
/// The cardinal rule of banking: Money must never disappear into thin air, nor be created from nothing.
/// If debiting Account A succeeds, but crediting Account B crashes (or vice versa), the database
/// MUST rollback so neither account is modified.
/// 
/// We enforce this via:
/// 1. An explicit database transaction wrapping both updates.
/// 2. Idempotency on the transfer reference_id to prevent double clicks.
/// 3. Aggregate root balance invariants.
/// </summary>
public class TransferService : ITransferService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransferRepository _transferRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<TransferRequest> _transferValidator;
    private readonly ILogger<TransferService> _logger;

    public TransferService(
        IAccountRepository accountRepository,
        ITransferRepository transferRepository,
        IUnitOfWork unitOfWork,
        IValidator<TransferRequest> transferValidator,
        ILogger<TransferService> logger)
    {
        _accountRepository = accountRepository;
        _transferRepository = transferRepository;
        _unitOfWork = unitOfWork;
        _transferValidator = transferValidator;
        _logger = logger;
    }

    public async Task<TransferDto> TransferMoneyAsync(TransferRequest request, CancellationToken ct = default)
    {
        // 1. Validate request parameters
        var validationResult = await _transferValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Open an atomic database transaction
        await using var txScope = await _unitOfWork.BeginTransactionAsync(ct);

        // 3. Idempotency check: ensure this transfer reference hasn't already been processed
        var transferExists = await _transferRepository.ExistsByReferenceIdAsync(request.ReferenceId, ct);
        if (transferExists)
        {
            _logger.LogWarning("Duplicate transfer request detected with ReferenceId '{ReferenceId}'", request.ReferenceId);
            throw new DuplicateReferenceException(request.ReferenceId, request.SourceAccountId);
        }

        // 4. Fetch and verify existence of both source and destination accounts
        var sourceAccount = await _accountRepository.GetByIdAsync(request.SourceAccountId, ct);
        if (sourceAccount == null)
        {
            _logger.LogWarning("Transfer failed: Source account {AccountId} not found", request.SourceAccountId);
            throw new AccountNotFoundException(request.SourceAccountId);
        }

        var destinationAccount = await _accountRepository.GetByIdAsync(request.DestinationAccountId, ct);
        if (destinationAccount == null)
        {
            _logger.LogWarning("Transfer failed: Destination account {AccountId} not found", request.DestinationAccountId);
            throw new AccountNotFoundException(request.DestinationAccountId);
        }

        // 5. Execute debit on source account (throws InsufficientBalanceException if balance < amount)
        var money = new Money(request.Amount);
        var debitRef = new ReferenceId($"TRF-OUT-{request.ReferenceId}");
        var debitTx = sourceAccount.Debit(
            money,
            debitRef,
            $"Transfer to Account {destinationAccount.Name} ({destinationAccount.Id}): {request.Description}"
        );

        // 6. Execute credit on destination account
        var creditRef = new ReferenceId($"TRF-IN-{request.ReferenceId}");
        var creditTx = destinationAccount.Credit(
            money,
            creditRef,
            $"Transfer from Account {sourceAccount.Name} ({sourceAccount.Id}): {request.Description}"
        );

        // 7. Create transfer audit record
        var transfer = Transfer.Create(
            sourceAccount.Id,
            destinationAccount.Id,
            money,
            new ReferenceId(request.ReferenceId),
            request.Description
        );
        transfer.LinkTransactions(debitTx.Id, creditTx.Id);

        await _transferRepository.AddAsync(transfer, ct);

        // 8. Commit the entire transaction atomically
        await _unitOfWork.SaveChangesAsync(ct);
        await txScope.CommitAsync(ct);

        _logger.LogInformation(
            "Transfer {TransferId} for {Amount:C} successfully executed from Account {SourceId} (New Bal: {SourceBal}) to Account {DestId} (New Bal: {DestBal})",
            transfer.Id, transfer.Amount, sourceAccount.Id, sourceAccount.Balance, destinationAccount.Id, destinationAccount.Balance);

        return new TransferDto
        {
            Id = transfer.Id,
            SourceAccountId = transfer.SourceAccountId,
            DestinationAccountId = transfer.DestinationAccountId,
            Amount = transfer.Amount,
            ReferenceId = transfer.ReferenceId,
            Description = transfer.Description,
            Status = transfer.Status.ToString().ToLowerInvariant(),
            SourceTransactionId = transfer.SourceTransactionId,
            DestinationTransactionId = transfer.DestinationTransactionId,
            SourceAccountNewBalance = sourceAccount.Balance,
            DestinationAccountNewBalance = destinationAccount.Balance,
            CreatedAt = transfer.CreatedAt
        };
    }
}
