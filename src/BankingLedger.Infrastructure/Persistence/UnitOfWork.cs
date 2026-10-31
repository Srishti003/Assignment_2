using BankingLedger.Application.Interfaces;
using BankingLedger.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace BankingLedger.Infrastructure.Persistence;

/// <summary>
/// Unit of Work implementation wrapping EF Core's change tracker and transactions.
/// 
/// Why this matters to PMs & Engineers:
/// In distributed or high-throughput banking apps, atomicity is non-negotiable.
/// If an error occurs midway through recording a transfer or transaction,
/// Unit of Work ensures the database rolls back to its exact prior state.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly BankingDbContext _context;

    public UnitOfWork(BankingDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Translate database constraint violation into clean Domain Exception
            throw new InvalidTransactionException(
                "A database integrity violation occurred: duplicate reference or conflicting entity already exists.");
        }
    }

    public async Task<IDbTransactionScope> BeginTransactionAsync(CancellationToken ct = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(ct);
        return new EfDbTransactionScope(transaction);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        if (ex.InnerException is PostgresException pgEx)
        {
            return pgEx.SqlState == PostgresErrorCodes.UniqueViolation;
        }
        return false;
    }
}

/// <summary>
/// Wraps EF Core's IDbContextTransaction as our decoupled IDbTransactionScope.
/// </summary>
internal sealed class EfDbTransactionScope : IDbTransactionScope
{
    private readonly IDbContextTransaction _transaction;
    private bool _committed;

    public EfDbTransactionScope(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        await _transaction.CommitAsync(ct);
        _committed = true;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        await _transaction.RollbackAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_committed)
        {
            try
            {
                await _transaction.RollbackAsync();
            }
            catch
            {
                // Transaction may already be closed/aborted
            }
        }
        await _transaction.DisposeAsync();
    }
}
