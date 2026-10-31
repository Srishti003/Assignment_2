using BankingLedger.Domain.Exceptions;

namespace BankingLedger.Domain.ValueObjects;

/// <summary>
/// Value Object encapsulating a unique transaction reference identifier.
/// 
/// Why this matters:
/// The reference_id is an idempotency key supplied by clients/external systems (e.g., checkout ID, payment gateway ref).
/// Encapsulating it ensures clean validation, sanitization (trimming), and consistent formatting across the domain.
/// </summary>
public sealed record ReferenceId
{
    public string Value { get; }

    public ReferenceId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidTransactionException("Reference ID cannot be empty or whitespace.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 100)
        {
            throw new InvalidTransactionException("Reference ID cannot exceed 100 characters.");
        }

        Value = trimmed;
    }

    public static implicit operator string(ReferenceId referenceId) => referenceId.Value;
    public static explicit operator ReferenceId(string value) => new(value);

    public override string ToString() => Value;
}
