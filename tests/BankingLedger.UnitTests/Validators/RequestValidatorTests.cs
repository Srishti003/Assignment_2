using BankingLedger.Application.Common;
using BankingLedger.Application.DTOs;
using BankingLedger.Application.Validators;
using FluentAssertions;
using Xunit;

namespace BankingLedger.UnitTests.Validators;

public class RequestValidatorTests
{
    private readonly CreateAccountRequestValidator _accountValidator = new();
    private readonly CreateTransactionRequestValidator _txValidator = new();
    private readonly TransferRequestValidator _transferValidator = new();
    private readonly PaginationParamsValidator _paginationValidator = new();

    [Fact]
    public void CreateAccount_EmptyName_ShouldFailValidation()
    {
        var request = new CreateAccountRequest { AccountName = "" };
        var result = _accountValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAccountRequest.AccountName));
    }

    [Theory]
    [InlineData("credit")]
    [InlineData("debit")]
    [InlineData("CREDIT")]
    [InlineData("DEBIT")]
    public void CreateTransaction_ValidTypes_ShouldPassValidation(string type)
    {
        var request = new CreateTransactionRequest
        {
            Type = type,
            Amount = 100m,
            ReferenceId = "VALID-REF"
        };
        var result = _txValidator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("deposit")]
    [InlineData("transfer")]
    [InlineData("invalid")]
    public void CreateTransaction_InvalidTypes_ShouldFailValidation(string invalidType)
    {
        var request = new CreateTransactionRequest
        {
            Type = invalidType,
            Amount = 100m,
            ReferenceId = "VALID-REF"
        };
        var result = _txValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTransactionRequest.Type));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void CreateTransaction_ZeroOrNegativeAmount_ShouldFailValidation(decimal invalidAmount)
    {
        var request = new CreateTransactionRequest
        {
            Type = "credit",
            Amount = invalidAmount,
            ReferenceId = "REF-1"
        };
        var result = _txValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTransactionRequest.Amount));
    }

    [Fact]
    public void Transfer_SameSourceAndDestination_ShouldFailValidation()
    {
        var accountId = Guid.NewGuid();
        var request = new TransferRequest
        {
            SourceAccountId = accountId,
            DestinationAccountId = accountId,
            Amount = 50m,
            ReferenceId = "REF-SAME"
        };
        var result = _transferValidator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 150)]
    public void Pagination_InvalidParams_ShouldFailValidation(int page, int limit)
    {
        var pagination = new PaginationParams { Page = page, Limit = limit };
        var result = _paginationValidator.Validate(pagination);
        result.IsValid.Should().BeFalse();
    }
}
