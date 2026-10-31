using BankingLedger.Domain.Exceptions;

namespace BankingLedger.Domain.ValueObjects;

/// <summary>
/// Value Object representing monetary values in the ledger.
/// 
/// Why Value Objects?
/// In banking applications, representing money with primitive floats or doubles can cause rounding errors.
/// By wrapping decimal within a Value Object, we enforce that:
/// 1. Negative amounts cannot even be instantiated for transactions.
/// 2. All amounts are normalized to 2 decimal places (cents/paise).
/// 3. Equality is based on monetary value, not object reference.
/// </summary>
public sealed record Money
{
    public decimal Amount { get; }

    public Money(decimal amount)
    {
        if (amount < 0)
        {
            throw new InvalidTransactionException("Monetary amount cannot be negative.");
        }

        // Standard financial normalization to 2 decimal places
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    public static Money Zero => new(0m);

    public static Money FromDecimal(decimal amount) => new(amount);

    public static implicit operator decimal(Money money) => money.Amount;
    public static explicit operator Money(decimal amount) => new(amount);

    public static Money operator +(Money a, Money b) => new(a.Amount + b.Amount);

    public static Money operator -(Money a, Money b)
    {
        if (a.Amount < b.Amount)
        {
            throw new InvalidTransactionException($"Cannot subtract {b.Amount} from {a.Amount} without resulting in a negative balance.");
        }
        return new(a.Amount - b.Amount);
    }

    public static bool operator >(Money a, Money b) => a.Amount > b.Amount;
    public static bool operator <(Money a, Money b) => a.Amount < b.Amount;
    public static bool operator >=(Money a, Money b) => a.Amount >= b.Amount;
    public static bool operator <=(Money a, Money b) => a.Amount <= b.Amount;

    public override string ToString() => Amount.ToString("F2");
}
