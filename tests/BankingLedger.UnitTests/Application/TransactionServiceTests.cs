using BankingLedger.Application.Common;
using BankingLedger.Application.DTOs;
using BankingLedger.Application.Interfaces;
using BankingLedger.Application.Services;
using BankingLedger.Application.Validators;
using BankingLedger.Domain.Entities;
using BankingLedger.Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BankingLedger.UnitTests.Application;

public class TransactionServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepoMock = new();
    private readonly Mock<ITransactionRepository> _txRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDbTransactionScope> _txScopeMock = new();
    private readonly TransactionService _sut;

    public TransactionServiceTests()
    {
        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_txScopeMock.Object);

        _sut = new TransactionService(
            _accountRepoMock.Object,
            _txRepoMock.Object,
            _uowMock.Object,
            new CreateTransactionRequestValidator(),
            new PaginationParamsValidator(),
            NullLogger<TransactionService>.Instance
        );
    }

    [Fact]
    public async Task CreateTransactionAsync_Credit_ShouldUpdateBalanceAndReturnDto()
    {
        // Arrange
        var account = Account.Create("Testing Credit", 50m);
        _accountRepoMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _txRepoMock.Setup(r => r.ExistsByReferenceIdAsync(account.Id, "REF-100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new CreateTransactionRequest
        {
            Type = "credit",
            Amount = 100m,
            ReferenceId = "REF-100",
            Description = "Deposit"
        };

        // Act
        var result = await _sut.CreateTransactionAsync(account.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be("credit");
        result.Amount.Should().Be(100m);
        result.BalanceAfter.Should().Be(150m);
        account.Balance.Should().Be(150m);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _txScopeMock.Verify(s => s.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTransactionAsync_Debit_WithSufficientBalance_ShouldDebitAndReturnDto()
    {
        // Arrange
        var account = Account.Create("Testing Debit", 100m);
        _accountRepoMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _txRepoMock.Setup(r => r.ExistsByReferenceIdAsync(account.Id, "REF-DEBIT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new CreateTransactionRequest
        {
            Type = "debit",
            Amount = 40m,
            ReferenceId = "REF-DEBIT",
            Description = "Groceries"
        };

        // Act
        var result = await _sut.CreateTransactionAsync(account.Id, request);

        // Assert
        result.Type.Should().Be("debit");
        result.Amount.Should().Be(40m);
        result.BalanceAfter.Should().Be(60m);
        account.Balance.Should().Be(60m);
    }

    [Fact]
    public async Task CreateTransactionAsync_AccountNotFound_ShouldThrowAccountNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _accountRepoMock.Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var request = new CreateTransactionRequest
        {
            Type = "credit",
            Amount = 50m,
            ReferenceId = "REF-MISSING"
        };

        // Act
        var act = () => _sut.CreateTransactionAsync(nonExistentId, request);

        // Assert
        await act.Should().ThrowAsync<AccountNotFoundException>()
            .Where(e => e.AccountId == nonExistentId);
    }

    [Fact]
    public async Task CreateTransactionAsync_DuplicateReferenceId_ShouldThrowDuplicateReferenceException()
    {
        // Arrange
        var account = Account.Create("Duplicate Test", 50m);
        _accountRepoMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _txRepoMock.Setup(r => r.ExistsByReferenceIdAsync(account.Id, "DUP-REF", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateTransactionRequest
        {
            Type = "credit",
            Amount = 25m,
            ReferenceId = "DUP-REF"
        };

        // Act
        var act = () => _sut.CreateTransactionAsync(account.Id, request);

        // Assert
        await act.Should().ThrowAsync<DuplicateReferenceException>()
            .Where(e => e.ReferenceId == "DUP-REF");
    }

    [Fact]
    public async Task CreateTransactionAsync_DebitWithInsufficientFunds_ShouldThrowInsufficientBalanceException()
    {
        // Arrange
        var account = Account.Create("Broke Account", 20m);
        _accountRepoMock.Setup(r => r.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _txRepoMock.Setup(r => r.ExistsByReferenceIdAsync(account.Id, "OVER-REF", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new CreateTransactionRequest
        {
            Type = "debit",
            Amount = 100m,
            ReferenceId = "OVER-REF"
        };

        // Act
        var act = () => _sut.CreateTransactionAsync(account.Id, request);

        // Assert
        await act.Should().ThrowAsync<InsufficientBalanceException>()
            .Where(e => e.CurrentBalance == 20m && e.RequestedAmount == 100m);
    }

    [Fact]
    public async Task GetTransactionsAsync_WithValidPagination_ShouldReturnPagedResult()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        _accountRepoMock.Setup(r => r.ExistsAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sampleTransactions = new List<TransactionDto>
        {
            new() { Id = Guid.NewGuid(), AccountId = accountId, Type = "credit", Amount = 100m, ReferenceId = "REF-1", CreatedAt = DateTimeOffset.UtcNow }
        };

        var expectedPagedResult = PagedResult<TransactionDto>.Create(sampleTransactions, 1, 1, 10);

        _txRepoMock.Setup(r => r.GetTransactionsByAccountIdAsync(accountId, It.IsAny<PaginationParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagedResult);

        // Act
        var result = await _sut.GetTransactionsAsync(accountId, new PaginationParams { Page = 1, Limit = 10 });

        // Assert
        result.Should().NotBeNull();
        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalRecords.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }
}
