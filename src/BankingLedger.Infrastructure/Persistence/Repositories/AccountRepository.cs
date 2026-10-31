using BankingLedger.Application.Interfaces;
using BankingLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingLedger.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core Repository for Account Aggregate Root.
/// </summary>
public class AccountRepository : IAccountRepository
{
    private readonly BankingDbContext _context;

    public AccountRepository(BankingDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<Account?> GetByIdWithTransactionsAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Accounts
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task AddAsync(Account account, CancellationToken ct = default)
    {
        await _context.Accounts.AddAsync(account, ct);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Accounts
            .AnyAsync(a => a.Id == id, ct);
    }

    public async Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Accounts
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }
}
