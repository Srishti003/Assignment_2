using BankingLedger.Application.Common;
using BankingLedger.Application.DTOs;
using BankingLedger.Application.Interfaces;
using BankingLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingLedger.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core Repository for Transaction queries and reference lookups.
/// </summary>
public class TransactionRepository : ITransactionRepository
{
    private readonly BankingDbContext _context;

    public TransactionRepository(BankingDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<TransactionDto>> GetTransactionsByAccountIdAsync(
        Guid accountId,
        PaginationParams pagination,
        CancellationToken ct = default)
    {
        var limit = pagination.SafeLimit;
        var skip = (pagination.Page - 1) * limit;

        var baseQuery = _context.Transactions
            .AsNoTracking()
            .Where(t => t.AccountId == accountId);

        // 1. Fetch total count of records matching the account
        var totalRecords = await baseQuery.CountAsync(ct);

        // 2. Fetch the paginated window ordered strictly by created_at DESC (latest first)
        var items = await baseQuery
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(limit)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                AccountId = t.AccountId,
                Type = t.Type.ToString().ToLowerInvariant(),
                Amount = t.Amount,
                ReferenceId = t.ReferenceId,
                Description = t.Description,
                BalanceAfter = t.BalanceAfter,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(ct);

        return PagedResult<TransactionDto>.Create(items, totalRecords, pagination.Page, limit);
    }

    public async Task<bool> ExistsByReferenceIdAsync(
        Guid accountId,
        string referenceId,
        CancellationToken ct = default)
    {
        return await _context.Transactions
            .AnyAsync(t => t.AccountId == accountId && t.ReferenceId == referenceId, ct);
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }
}
