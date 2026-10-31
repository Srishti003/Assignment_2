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

public class TransferServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepoMock = new();
    private readonly Mock<ITransferRepository> _transferRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IDbTransactionScope> _txScopeMock = new();
    private readonly TransferService _sut;

    public TransferServiceTests()
    {
        _uowMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_txScopeMock.Object);

        _sut = new TransferService(
            _accountRepoMock.Object,
            _transferRepoMock.Object,
            _uowMock.Object,
            new TransferRequestValidator(),
            NullLogger<TransferService>.Instance
        );
    }

    [Fact]
    public async Task TransferMoneyAsync_ValidTransfer_ShouldDebitSourceAndCreditDestination()
    {
        // Assessment bonus example:
        // Account A balance: 100
        // Account B balance: 50
        // Transfer: 30 from A to B
        // Account A balance: 70
        // Account B balance: 80

        // Arrange
        var accountA = Account.Create("Account A", 100m);
        var accountB = Account.Create("Account B", 50m);

        _accountRepoMock.Setup(r => r.GetByIdAsync(accountA.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountA);
        _accountRepoMock.Setup(r => r.GetByIdAsync(accountB.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountB);
        _transferRepoMock.Setup(r => r.ExistsByReferenceIdAsync("TRF-30", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new TransferRequest
        {
            SourceAccountId = accountA.Id,
            DestinationAccountId = accountB.Id,
            Amount = 30m,
            ReferenceId = "TRF-30",
            Description = "Settling bill"
        };

        // Act
        var result = await _sut.TransferMoneyAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Amount.Should().Be(30m);
        result.SourceAccountNewBalance.Should().Be(70m);
        result.DestinationAccountNewBalance.Should().Be(80m);

        accountA.Balance.Should().Be(70m);
        accountB.Balance.Should().Be(80m);

        _transferRepoMock.Verify(t => t.AddAsync(It.IsAny<Transfer>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _txScopeMock.Verify(s => s.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransferMoneyAsync_InsufficientSourceFunds_ShouldThrowAndNotModifyBalances()
    {
        // Arrange
        var accountA = Account.Create("Account A", 20m);
        var accountB = Account.Create("Account B", 50m);

        _accountRepoMock.Setup(r => r.GetByIdAsync(accountA.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountA);
        _accountRepoMock.Setup(r => r.GetByIdAsync(accountB.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountB);

        var request = new TransferRequest
        {
            SourceAccountId = accountA.Id,
            DestinationAccountId = accountB.Id,
            Amount = 50m,
            ReferenceId = "TRF-OVER"
        };

        // Act
        var act = () => _sut.TransferMoneyAsync(request);

        // Assert
        await act.Should().ThrowAsync<InsufficientBalanceException>();
        accountA.Balance.Should().Be(20m);
        accountB.Balance.Should().Be(50m);
        _txScopeMock.Verify(s => s.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
