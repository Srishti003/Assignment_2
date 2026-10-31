using BankingLedger.Domain.Common;
using BankingLedger.Domain.Enums;
using BankingLedger.Domain.Exceptions;
using BankingLedger.Domain.ValueObjects;

namespace BankingLedger.Domain.Entities;

/// <summary>
/// The Account Aggregate Root.
/// 
/// Why this is the core of our Domain-Driven Design (DDD):
/// In banking software, balance consistency is paramount. A common bug in naive systems
/// occurs when an application writes a transaction row in one table, and updates a balance
/// column in another, but doesn't coordinate the business rules within an Aggregate boundary.
/// 
/// Here, Account owns all state mutations:
/// - Balance can NEVER be modified from the outside.
/// - Every balance change MUST produce an immutable Transaction item.
/// - Overdrafts (insufficient balance) are blocked at the domain boundary before touching any database.
/// - Concurrency tokens (RowVersion) protect against race conditions when two debits hit simultaneously.
/// </summary>
public sealed class Account : BaseEntity<Guid>, IAggregateRoot
{
    private readonly List<Transaction> _transactions = new();

    public string Name { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Optimistic concurrency token (xmin in PostgreSQL or RowVersion).
    /// Prevents the "lost update" problem if two simultaneous requests attempt to debit the same account.
    /// </summary>
    public uint RowVersion { get; private set; }

    /// <summary>
    /// Read-only view of transactions. Child entities can only be appended through Credit/Debit methods.
    /// </summary>
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    // Private parameterless constructor for EF Core materialization
    private Account()
    {
    }

    /// <summary>
    /// Factory method to create a new Account aggregate root.
    /// </summary>
    public static Account Create(string name, decimal initialBalance = 0m)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidTransactionException("Account name must be provided and cannot be empty.");
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > 200)
        {
            throw new InvalidTransactionException("Account name cannot exceed 200 characters.");
        }

        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            Balance = 0m,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // If an initial balance is specified upon account opening,
        // we record it as a formal initial deposit transaction to maintain 100% balance consistency!
        if (initialBalance > 0m)
        {
            account.Credit(
                new Money(initialBalance),
                new ReferenceId($"INIT-{account.Id.ToString()[..8].ToUpperInvariant()}"),
                "Initial account opening balance deposit"
            );
        }
        else if (initialBalance < 0m)
        {
            throw new InvalidTransactionException("Initial account balance cannot be negative.");
        }

        return account;
    }

    /// <summary>
    /// Credits money to the account (Deposit / Inflow).
    /// Atomically updates the aggregate's balance and appends an immutable transaction.
    /// </summary>
    public Transaction Credit(Money amount, ReferenceId referenceId, string? description = null)
    {
        if (amount <= Money.Zero)
        {
            throw new InvalidTransactionException("Credit amount must be greater than zero.");
        }

        // Calculate new balance
        var newBalance = new Money(Balance) + amount;
        Balance = newBalance.Amount;
        UpdatedAt = DateTimeOffset.UtcNow;

        var transaction = new Transaction(
            accountId: Id,
            type: TransactionType.Credit,
            amount: amount,
            referenceId: referenceId,
            description: description,
            balanceAfter: newBalance
        );

        _transactions.Add(transaction);
        return transaction;
    }

    /// <summary>
    /// Debits money from the account (Withdrawal / Outflow).
    /// Enforces the business invariant: Account balance can never fall below zero.
    /// </summary>
    public Transaction Debit(Money amount, ReferenceId referenceId, string? description = null)
    {
        if (amount <= Money.Zero)
        {
            throw new InvalidTransactionException("Debit amount must be greater than zero.");
        }

        // Domain Rule: Insufficient balance check
        if (Balance < amount.Amount)
        {
            throw new InsufficientBalanceException(Id, Balance, amount.Amount);
        }

        // Calculate new balance safely
        var newBalance = new Money(Balance) - amount;
        Balance = newBalance.Amount;
        UpdatedAt = DateTimeOffset.UtcNow;

        var transaction = new Transaction(
            accountId: Id,
            type: TransactionType.Debit,
            amount: amount,
            referenceId: referenceId,
            description: description,
            balanceAfter: newBalance
        );

        _transactions.Add(transaction);
        return transaction;
    }

    /// <summary>
    /// Validates whether the calculated sum of transactions matches the stored Balance.
    /// This provides a business verification check for financial audit integrity.
    /// </summary>
    public bool VerifyBalanceConsistency()
    {
        decimal calculated = 0m;
        foreach (var tx in _transactions.OrderBy(t => t.CreatedAt))
        {
            if (tx.Type == TransactionType.Credit)
                calculated += tx.Amount;
            else if (tx.Type == TransactionType.Debit)
                calculated -= tx.Amount;
        }

        return Math.Round(calculated, 2) == Math.Round(Balance, 2);
    }
}
