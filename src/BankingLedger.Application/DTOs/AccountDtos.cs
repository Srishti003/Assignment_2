using System.ComponentModel.DataAnnotations;

namespace BankingLedger.Application.DTOs;

/// <summary>
/// Payload to open a new account in the ledger.
/// </summary>
public record CreateAccountRequest
{
    /// <summary>
    /// Account owner / display name (e.g. "Alice Johnson" or "Operating Reserve Account").
    /// </summary>
    [Required]
    public string AccountName { get; init; } = string.Empty;

    /// <summary>
    /// Optional initial deposit when opening the account. Defaults to 0.
    /// </summary>
    public decimal? InitialBalance { get; init; } = 0m;
}

/// <summary>
/// Data Transfer Object representing account details returned to clients.
/// </summary>
public record AccountDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// Lightweight DTO for quick balance checks.
/// </summary>
public record AccountBalanceDto
{
    public Guid AccountId { get; init; }
    public decimal Balance { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
