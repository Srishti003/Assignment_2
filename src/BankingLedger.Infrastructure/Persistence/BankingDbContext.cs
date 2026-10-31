using BankingLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingLedger.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for the Banking Ledger.
/// 
/// Business Context:
/// Serves as the gateway between our domain models and PostgreSQL storage.
/// In accordance with Domain-Driven Design (DDD):
/// - It applies fluent configurations containing database check constraints and composite unique indexes.
/// - It supports optimistic concurrency using PostgreSQL's xmin system column.
/// </summary>
public class BankingDbContext : DbContext
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Transfer> Transfers => Set<Transfer>();

    public BankingDbContext(DbContextOptions<BankingDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Automatically discover and apply all IEntityTypeConfiguration classes in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BankingDbContext).Assembly);
    }
}
