using BankingLedger.Domain.Common;
using BankingLedger.Domain.Enums;
using BankingLedger.Domain.Exceptions;
using BankingLedger.Domain.ValueObjects;

namespace BankingLedger.Domain.Entities;

/// <summary>
/// Represents a high-level money transfer between two accounts.
/// 
/// Business Context:
/// When Account A sends \$30 to Account B, this Transfer record coordinates the lifecycle.
/// Under the hood, this generates two linked atomic ledger transactions:
/// 1. A Debit on Account A for \$30
/// 2. A Credit on Account B for \$30
/// If either leg fails, the entire database transaction is aborted.
/// </summary>
public sealed class Transfer : BaseEntity<Guid>
{
    public Guid SourceAccountId { get; private set; }
    public Guid DestinationAccountId { get; private set; }
    public decimal Amount { get; private set; }
    public string ReferenceId { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TransferStatus Status { get; private set; }
    public Guid? SourceTransactionId { get; private set; }
    public Guid? DestinationTransactionId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Transfer()
    {
    }

    public static Transfer Create(
        Guid sourceAccountId,
        Guid destinationAccountId,
        Money amount,
        ReferenceId referenceId,
        string? description)
    {
        if (sourceAccountId == destinationAccountId)
        {
            throw new SelfTransferException(sourceAccountId);
        }

        if (amount <= Money.Zero)
        {
            throw new InvalidTransactionException("Transfer amount must be strictly greater than 0.");
        }

        return new Transfer
        {
            Id = Guid.NewGuid(),
            SourceAccountId = sourceAccountId,
            DestinationAccountId = destinationAccountId,
            Amount = amount.Amount,
            ReferenceId = referenceId.Value,
            Description = string.IsNullOrWhiteSpace(description) ? "Account to Account Transfer" : description.Trim(),
            Status = TransferStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void LinkTransactions(Guid sourceTransactionId, Guid destinationTransactionId)
    {
        SourceTransactionId = sourceTransactionId;
        DestinationTransactionId = destinationTransactionId;
    }
}
