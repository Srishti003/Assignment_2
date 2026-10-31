using BankingLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingLedger.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for the Account Aggregate Root.
/// 
/// Business Context:
/// Database constraints serve as the ultimate safety net. Even if someone bypassed the API
/// and tried to run a raw SQL query, the database check constraint enforces that
/// account balances can never be negative.
/// </summary>
public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts", t =>
        {
            t.HasCheckConstraint("CK_accounts_balance_non_negative", "\"Balance\" >= 0");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Balance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .IsRequired();

        // Concurrency token to protect against race conditions
        builder.Property(a => a.RowVersion)
            .IsRowVersion();

        // Navigation configuration: transactions are accessible through Account aggregate root
        builder.HasMany(a => a.Transactions)
            .WithOne(t => t.Account)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tell EF Core to use backing field for _transactions
        builder.Navigation(a => a.Transactions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
