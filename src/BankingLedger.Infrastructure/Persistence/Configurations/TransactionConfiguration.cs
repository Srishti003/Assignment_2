using BankingLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingLedger.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for the Transaction Entity.
/// 
/// Data Integrity & Performance:
/// 1. Check constraint: Amount must be > 0.
/// 2. Unique index on (AccountId, ReferenceId):
///    Guarantees idempotency at the database storage level. Two simultaneous threads
///    attempting to post the same reference_id will encounter an atomic uniqueness rejection.
/// 3. Composite Index on (AccountId, CreatedAt DESC):
///    Optimizes the pagination and latest-first sorting requirement without full-table scans.
/// </summary>
public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions", t =>
        {
            t.HasCheckConstraint("CK_transactions_amount_positive", "\"Amount\" > 0");
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.AccountId)
            .IsRequired();

        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(t => t.ReferenceId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(500)
            .HasDefaultValue(string.Empty);

        builder.Property(t => t.BalanceAfter)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        // Database-enforced idempotency per account
        builder.HasIndex(t => new { t.AccountId, t.ReferenceId })
            .IsUnique()
            .HasDatabaseName("IX_transactions_account_reference_unique");

        // Fast sorting index for latest-first pagination queries
        builder.HasIndex(t => new { t.AccountId, t.CreatedAt })
            .HasDatabaseName("IX_transactions_account_created_at");
    }
}
