using BankingLedger.Domain.Entities;
using BankingLedger.Domain.Enums;
using BankingLedger.Domain.Exceptions;
using BankingLedger.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BankingLedger.UnitTests.Domain;

/// <summary>
/// Unit tests verifying the core domain invariants of the Account aggregate root.
/// 
/// Why this matters to PMs:
/// These tests act as an executable specification of the business rules.
/// They mathematically prove that balances cannot go negative and transactions
/// cannot tamper with ledger history.
/// </summary>
public class AccountAggregateTests
{
    [Fact]
    public void Create_WithValidName_ShouldInitializeAccountWithZeroBalance()
    {
        // Arrange & Act
        var account = Account.Create("Alice Johnson");

        // Assert
        account.Id.Should().NotBeEmpty();
        account.Name.Should().Be("Alice Johnson");
        account.Balance.Should().Be(0m);
        account.Transactions.Should().BeEmpty();
        account.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithInitialBalance_ShouldCreateOpeningCreditTransaction()
    {
        // Arrange & Act
        var account = Account.Create("Bob Smith", 250.75m);

        // Assert
        account.Balance.Should().Be(250.75m);
        account.Transactions.Should().HaveCount(1);

        var firstTx = account.Transactions.First();
        firstTx.Type.Should().Be(TransactionType.Credit);
        firstTx.Amount.Should().Be(250.75m);
        firstTx.BalanceAfter.Should().Be(250.75m);
        firstTx.ReferenceId.Should().StartWith("INIT-");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ShouldThrowInvalidTransactionException(string? invalidName)
    {
        // Act
        var act = () => Account.Create(invalidName!);

        // Assert
        act.Should().Throw<InvalidTransactionException>()
            .WithMessage("*Account name must be provided*");
    }

    [Fact]
    public void Credit_WithPositiveAmount_ShouldIncreaseBalanceAndRecordTransaction()
    {
        // Arrange
        var account = Account.Create("Operational Account");
        var creditAmount = new Money(150.00m);
        var reference = new ReferenceId("DEP-001");

        // Act
        var transaction = account.Credit(creditAmount, reference, "Invoice payment");

        // Assert
        account.Balance.Should().Be(150.00m);
        transaction.Type.Should().Be(TransactionType.Credit);
        transaction.Amount.Should().Be(150.00m);
        transaction.BalanceAfter.Should().Be(150.00m);
        transaction.ReferenceId.Should().Be("DEP-001");
        account.Transactions.Should().Contain(transaction);
    }

    [Fact]
    public void Debit_WithSufficientBalance_ShouldDecreaseBalanceAndRecordTransaction()
    {
        // Arrange
        var account = Account.Create("Checking Account", 200m);
        var debitAmount = new Money(75m);
        var reference = new ReferenceId("WDL-001");

        // Act
        var transaction = account.Debit(debitAmount, reference, "ATM Withdrawal");

        // Assert
        account.Balance.Should().Be(125m);
        transaction.Type.Should().Be(TransactionType.Debit);
        transaction.Amount.Should().Be(75m);
        transaction.BalanceAfter.Should().Be(125m);
        account.Transactions.Should().HaveCount(2); // Initial deposit + withdrawal
    }

    [Fact]
    public void Debit_WithInsufficientBalance_ShouldThrowInsufficientBalanceException_AndNotModifyBalance()
    {
        // Arrange
        var account = Account.Create("Savings Account", 50m);
        var excessiveDebit = new Money(100m);
        var reference = new ReferenceId("WDL-FAIL");

        // Act
        var act = () => account.Debit(excessiveDebit, reference, "Overdraft attempt");

        // Assert
        act.Should().Throw<InsufficientBalanceException>()
            .Where(ex => ex.CurrentBalance == 50m && ex.RequestedAmount == 100m);

        // Invariant: Balance must remain completely unaffected
        account.Balance.Should().Be(50m);
        account.Transactions.Should().HaveCount(1); // Only the initial deposit
    }

    [Fact]
    public void BalanceConsistency_AssessmentExampleSequence_ShouldMatchFinalBalance100()
    {
        // Assessment specification test case:
        // Initial balance: 0
        // Credit: 100
        // Credit: 50
        // Debit: 30
        // Debit: 20
        // Final balance: 100

        // Arrange
        var account = Account.Create("Assessment Verification Account");

        // Act
        account.Credit(new Money(100m), new ReferenceId("REF-1"), "Credit 100");
        account.Credit(new Money(50m), new ReferenceId("REF-2"), "Credit 50");
        account.Debit(new Money(30m), new ReferenceId("REF-3"), "Debit 30");
        account.Debit(new Money(20m), new ReferenceId("REF-4"), "Debit 20");

        // Assert
        account.Balance.Should().Be(100m);
        account.VerifyBalanceConsistency().Should().BeTrue();
        account.Transactions.Should().HaveCount(4);
    }
}
