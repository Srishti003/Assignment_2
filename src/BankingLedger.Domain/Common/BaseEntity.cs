namespace BankingLedger.Domain.Common;

/// <summary>
/// Marker interface to indicate that an entity is an Aggregate Root in DDD.
/// 
/// Why this matters to the business & PMs:
/// An Aggregate Root acts as the "front door" and single source of truth for a cluster of related data.
/// By enforcing that external services can only manipulate child entities (like Transactions)
/// through their Aggregate Root (Account), we guarantee business rules (like non-negative balances)
/// can NEVER be bypassed by a rogue query or shortcut in code.
/// </summary>
public interface IAggregateRoot
{
}

/// <summary>
/// Base class for domain entities providing identity equality.
/// In Domain-Driven Design, entities are defined by their identity (Id), not just their attributes.
/// </summary>
public abstract class BaseEntity<TId>
{
    public TId Id { get; protected set; } = default!;

    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity<TId> other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (EqualityComparer<TId>.Default.Equals(Id, default) || 
            EqualityComparer<TId>.Default.Equals(other.Id, default))
            return false;

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<TId>.Default.Equals(Id, default) 
            ? base.GetHashCode() 
            : EqualityComparer<TId>.Default.GetHashCode(Id!);
    }

    public static bool operator ==(BaseEntity<TId>? left, BaseEntity<TId>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(BaseEntity<TId>? left, BaseEntity<TId>? right)
    {
        return !Equals(left, right);
    }
}
