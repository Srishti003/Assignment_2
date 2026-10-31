using System.ComponentModel.DataAnnotations;

namespace BankingLedger.Application.DTOs;

/// <summary>
/// Payload to post a transaction against an account.
/// </summary>
public record CreateTransactionRequest
{
    /// <summary>
    /// Type of transaction: "credit" or "debit" (case-insensitive).
    /// </summary>
    [Required]
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Monetary amount. Must be strictly greater than 0.
    /// </summary>
    [Required]
    public decimal Amount { get; init; }

    /// <summary>
    /// Unique reference identifier for idempotency and external tracking.
    /// (e.g. invoice number, payment gateway charge ID, or client-generated UUID).
    /// </summary>
    [Required]
    public string ReferenceId { get; init; } = string.Empty;

    /// <summary>
    /// Optional human-readable description (e.g. "Monthly salary credit", "Coffee shop debit").
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Data Transfer Object representing an immutable transaction returned to clients.
/// </summary>
public record TransactionDto
{
    public Guid Id { get; init; }
    public Guid AccountId { get; init; }
    public string Type { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string ReferenceId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal BalanceAfter { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
