namespace BankingLedger.Domain.Exceptions;

/// <summary>
/// Base exception for all domain business-rule violations.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}

/// <summary>
/// Thrown when an account does not have adequate funds for a debit or transfer.
/// 
/// Business Impact:
/// Prevents the customer account from going into negative balance (overdraft protection).
/// </summary>
public class InsufficientBalanceException : DomainException
{
    public Guid AccountId { get; }
    public decimal CurrentBalance { get; }
    public decimal RequestedAmount { get; }

    public InsufficientBalanceException(Guid accountId, decimal currentBalance, decimal requestedAmount)
        : base($"Account {accountId} has insufficient funds. Current balance: {currentBalance:C}, Requested amount: {requestedAmount:C}")
    {
        AccountId = accountId;
        CurrentBalance = currentBalance;
        RequestedAmount = requestedAmount;
    }
}

/// <summary>
/// Thrown when an account is not found in the ledger.
/// </summary>
public class AccountNotFoundException : DomainException
{
    public Guid AccountId { get; }

    public AccountNotFoundException(Guid accountId)
        : base($"Account with ID '{accountId}' was not found.")
    {
        AccountId = accountId;
    }
}

/// <summary>
/// Thrown when a duplicate reference_id is provided for an account.
/// 
/// Business Impact:
/// Idempotency & fraud prevention! If a network timeout causes a client to retry a request,
/// detecting the duplicate reference_id stops double-charging or double-crediting customer funds.
/// </summary>
public class DuplicateReferenceException : DomainException
{
    public string ReferenceId { get; }
    public Guid AccountId { get; }

    public DuplicateReferenceException(string referenceId, Guid accountId)
        : base($"A transaction with Reference ID '{referenceId}' already exists for Account '{accountId}'.")
    {
        ReferenceId = referenceId;
        AccountId = accountId;
    }
}

/// <summary>
/// Thrown when transaction parameters violate domain invariants (e.g. non-positive amount).
/// </summary>
public class InvalidTransactionException : DomainException
{
    public InvalidTransactionException(string message) : base(message)
    {
    }
}

/// <summary>
/// Thrown when a user attempts to transfer money to the same account.
/// </summary>
public class SelfTransferException : DomainException
{
    public Guid AccountId { get; }

    public SelfTransferException(Guid accountId)
        : base($"Cannot initiate a money transfer where source and destination account are identical ('{accountId}').")
    {
        AccountId = accountId;
    }
}
