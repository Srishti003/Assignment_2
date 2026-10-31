using BankingLedger.Application.Interfaces;
using BankingLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingLedger.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core Repository for Inter-Account Transfers.
/// </summary>
public class TransferRepository : ITransferRepository
{
    private readonly BankingDbContext _context;

    public TransferRepository(BankingDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Transfer transfer, CancellationToken ct = default)
    {
        await _context.Transfers.AddAsync(transfer, ct);
    }

    public async Task<Transfer?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Transfers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<bool> ExistsByReferenceIdAsync(string referenceId, CancellationToken ct = default)
    {
        return await _context.Transfers
            .AnyAsync(t => t.ReferenceId == referenceId, ct);
    }
}
