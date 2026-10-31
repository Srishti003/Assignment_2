using BankingLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankingLedger.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for Transfer Entity.
/// </summary>
public class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("transfers", t =>
        {
            t.HasCheckConstraint("CK_transfers_amount_positive", "\"Amount\" > 0");
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.SourceAccountId)
            .IsRequired();

        builder.Property(t => t.DestinationAccountId)
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

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        // Unique reference for transfers to prevent duplicate requests
        builder.HasIndex(t => t.ReferenceId)
            .IsUnique()
            .HasDatabaseName("IX_transfers_reference_unique");
    }
}
