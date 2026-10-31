namespace BankingLedger.Domain.Enums;

/// <summary>
/// Defines the nature of a ledger transaction.
/// 
/// Business Context:
/// - Credit: Inflow of funds into the customer's account (e.g., deposit, refund, incoming transfer).
/// - Debit: Outflow of funds from the customer's account (e.g., withdrawal, payment, outgoing transfer).
/// </summary>
public enum TransactionType
{
    Credit = 1,
    Debit = 2
}
