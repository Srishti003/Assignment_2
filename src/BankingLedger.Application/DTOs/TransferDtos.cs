using System.ComponentModel.DataAnnotations;

namespace BankingLedger.Application.DTOs;

/// <summary>
/// Payload to initiate an atomic transfer between two accounts.
/// </summary>
public record TransferRequest
{
    /// <summary>
    /// Account from which money will be debited.
    /// </summary>
    [Required]
    public Guid SourceAccountId { get; init; }

    /// <summary>
    /// Account to which money will be credited.
    /// </summary>
    [Required]
    public Guid DestinationAccountId { get; init; }

    /// <summary>
    /// Monetary amount to transfer. Must be strictly greater than 0.
    /// </summary>
    [Required]
    public decimal Amount { get; init; }

    /// <summary>
    /// Unique idempotency reference key for this transfer.
    /// Prevents accidental double transfers if user clicks "Transfer" twice.
    /// </summary>
    [Required]
    public string ReferenceId { get; init; } = string.Empty;

    /// <summary>
    /// Optional transfer memo or description (e.g. "Dinner split", "Rent payment").
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Response payload after a successful atomic transfer.
/// </summary>
public record TransferDto
{
    public Guid Id { get; init; }
    public Guid SourceAccountId { get; init; }
    public Guid DestinationAccountId { get; init; }
    public decimal Amount { get; init; }
    public string ReferenceId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid? SourceTransactionId { get; init; }
    public Guid? DestinationTransactionId { get; init; }
    public decimal SourceAccountNewBalance { get; init; }
    public decimal DestinationAccountNewBalance { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
