using BankingLedger.Application.Common;
using BankingLedger.Application.DTOs;
using FluentValidation;

namespace BankingLedger.Application.Validators;

/// <summary>
/// Validator for Account Creation requests.
/// </summary>
public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.AccountName)
            .NotEmpty().WithMessage("Account name must be provided and cannot be whitespace.")
            .MaximumLength(200).WithMessage("Account name cannot exceed 200 characters.");

        RuleFor(x => x.InitialBalance)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.InitialBalance.HasValue)
            .WithMessage("Initial balance cannot be negative.");
    }
}

/// <summary>
/// Validator for Transaction Creation requests.
/// </summary>
public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionRequestValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Transaction type must be provided.")
            .Must(t => t.Equals("credit", StringComparison.OrdinalIgnoreCase) || 
                       t.Equals("debit", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Transaction type must be either 'credit' or 'debit'.");

        RuleFor(x => x.Amount)
            .GreaterThan(0m).WithMessage("Amount must be strictly greater than 0.")
            .PrecisionScale(18, 2, false).WithMessage("Amount cannot have more than 2 decimal places.");

        RuleFor(x => x.ReferenceId)
            .NotEmpty().WithMessage("reference_id must be provided and cannot be empty.")
            .MaximumLength(100).WithMessage("reference_id cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

/// <summary>
/// Validator for Inter-Account Money Transfer requests.
/// </summary>
public class TransferRequestValidator : AbstractValidator<TransferRequest>
{
    public TransferRequestValidator()
    {
        RuleFor(x => x.SourceAccountId)
            .NotEmpty().WithMessage("Source account ID is required.");

        RuleFor(x => x.DestinationAccountId)
            .NotEmpty().WithMessage("Destination account ID is required.");

        RuleFor(x => x)
            .Must(x => x.SourceAccountId != x.DestinationAccountId)
            .WithMessage("Source account and destination account cannot be the same.");

        RuleFor(x => x.Amount)
            .GreaterThan(0m).WithMessage("Transfer amount must be strictly greater than 0.")
            .PrecisionScale(18, 2, false).WithMessage("Transfer amount cannot have more than 2 decimal places.");

        RuleFor(x => x.ReferenceId)
            .NotEmpty().WithMessage("reference_id must be provided for tracking and idempotency.")
            .MaximumLength(100).WithMessage("reference_id cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

/// <summary>
/// Validator for Pagination query parameters.
/// </summary>
public class PaginationParamsValidator : AbstractValidator<PaginationParams>
{
    public PaginationParamsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page parameter must be an integer greater than or equal to 1.");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100).WithMessage("Limit parameter must be between 1 and 100.");
    }
}
