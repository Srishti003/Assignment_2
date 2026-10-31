using BankingLedger.Application.Common;
using BankingLedger.Application.DTOs;
using BankingLedger.Domain.Entities;

namespace BankingLedger.Application.Interfaces;

/// <summary>
/// Repository interface for Account aggregate root persistence.
/// </summary>
public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Account?> GetByIdWithTransactionsAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Account account, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken ct = default);
}

/// <summary>
/// Repository interface for Transaction read operations and reference_id verification.
/// </summary>
public interface ITransactionRepository
{
    Task<PagedResult<TransactionDto>> GetTransactionsByAccountIdAsync(
        Guid accountId,
        PaginationParams pagination,
        CancellationToken ct = default);

    Task<bool> ExistsByReferenceIdAsync(
        Guid accountId,
        string referenceId,
        CancellationToken ct = default);

    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Repository interface for Inter-Account Transfers.
/// </summary>
public interface ITransferRepository
{
    Task AddAsync(Transfer transfer, CancellationToken ct = default);
    Task<Transfer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByReferenceIdAsync(string referenceId, CancellationToken ct = default);
}

/// <summary>
/// Unit of Work and explicit transaction management.
/// 
/// Business Context:
/// Atomicity requirement: "Creating a transaction and updating the account balance must be treated as one atomic operation.
/// If either operation fails, neither change should be persisted."
/// 
/// The Unit of Work abstracts EF Core's DbContext transaction boundary so application use-cases
/// can guarantee all-or-nothing commits without leaking ORM details into domain logic.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<IDbTransactionScope> BeginTransactionAsync(CancellationToken ct = default);
}

public interface IDbTransactionScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
