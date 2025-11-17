namespace Hartonomous.Data.Entities;

/// <summary>
/// Lightweight projection interface - just ID, no navigation properties.
/// </summary>
/// <typeparam name="TKey">Primary key type</typeparam>
/// <remarks>
/// USE CASE: Pass IIdentifiable&lt;long&gt; to services instead of full entity graph.
/// Example: ProcessAtom(IIdentifiable&lt;long&gt; atom) only needs AtomId, not all relationships.
/// Prevents N+1 queries, reduces memory, improves performance.
/// 
/// IMPLEMENTATION: Add partial class for scaffolded entities:
/// <code>
/// public partial class Atom : IIdentifiable&lt;long&gt;
/// {
///     long IIdentifiable&lt;long&gt;.Id => AtomId; // Map to actual PK column
/// }
/// </code>
/// </remarks>
public interface IIdentifiable<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Primary key identifier.
    /// </summary>
    TKey Id { get; }
}

/// <summary>
/// Projection with ID and timestamp - useful for temporal queries without full entity.
/// </summary>
/// <typeparam name="TKey">Primary key type</typeparam>
public interface ITimestamped<TKey> : IIdentifiable<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    /// When the entity was created (for temporal queries, ordering, filtering).
    /// </summary>
    DateTime CreatedAt { get; }
}

/// <summary>
/// Projection with ID and tenant - for multi-tenant filtering without loading full entity.
/// </summary>
/// <typeparam name="TKey">Primary key type</typeparam>
public interface ITenantScoped<TKey> : IIdentifiable<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Tenant identifier for multi-tenant isolation.
    /// </summary>
    int TenantId { get; }
}
