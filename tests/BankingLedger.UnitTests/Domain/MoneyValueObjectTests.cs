using BankingLedger.Domain.Exceptions;
using BankingLedger.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BankingLedger.UnitTests.Domain;

public class MoneyValueObjectTests
{
    [Fact]
    public void Money_ShouldRoundToTwoDecimalPlaces()
    {
        var money = new Money(10.555m);
        money.Amount.Should().Be(10.56m);
    }

    [Fact]
    public void Money_WithNegativeAmount_ShouldThrowInvalidTransactionException()
    {
        var act = () => new Money(-5m);
        act.Should().Throw<InvalidTransactionException>()
            .WithMessage("*cannot be negative*");
    }

    [Fact]
    public void Money_AdditionAndSubtraction_ShouldBeAccurate()
    {
        var m1 = new Money(100.50m);
        var m2 = new Money(49.25m);

        var sum = m1 + m2;
        var diff = m1 - m2;

        sum.Amount.Should().Be(149.75m);
        diff.Amount.Should().Be(51.25m);
    }
}
