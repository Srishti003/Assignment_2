using BankingLedger.Domain.Common;
using BankingLedger.Domain.Enums;
using BankingLedger.Domain.Exceptions;
using BankingLedger.Domain.ValueObjects;

namespace BankingLedger.Domain.Entities;

/// <summary>
/// Represents an immutable ledger entry.
/// 
/// Business Context:
/// In an accounting or banking ledger, transactions are write-once append-only records.
/// Once written, a transaction cannot be edited or deleted. If a mistake was made in the real world,
/// accountants do not delete the row; they issue a compensating counter-transaction (reversal).
/// 
/// All setters are private/init-only so that neither application code nor API controllers
/// can tamper with financial history.
/// </summary>
public sealed class Transaction : BaseEntity<Guid>
{
    public Guid AccountId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string ReferenceId { get; private set; } = string.Empty;
    public decimal BalanceAfter { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation property for EF Core
    public Account? Account { get; private set; }

    // Private parameterless constructor for EF Core materialization
    private Transaction()
    {
    }

    /// <summary>
    /// Internal constructor: transactions should only be created via the Account Aggregate Root!
    /// This guarantees that an isolated transaction cannot exist without updating the account balance.
    /// </summary>
    internal Transaction(
        Guid accountId,
        TransactionType type,
        Money amount,
        ReferenceId referenceId,
        string? description,
        Money balanceAfter,
        DateTimeOffset? createdAt = null)
    {
        if (amount <= Money.Zero)
        {
            throw new InvalidTransactionException("Transaction amount must be strictly greater than 0.");
        }

        Id = Guid.NewGuid();
        AccountId = accountId;
        Type = type;
        Amount = amount.Amount;
        ReferenceId = referenceId.Value;
        Description = string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim();
        BalanceAfter = balanceAfter.Amount;
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
    }
}
