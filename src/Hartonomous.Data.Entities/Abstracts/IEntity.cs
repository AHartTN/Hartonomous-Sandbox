namespace Hartonomous.Data.Entities;

/// <summary>
/// Marker interface for all entities - enables generic repository/service constraints.
/// Use for DI: IRepository&lt;TEntity&gt; where TEntity : IEntity
/// </summary>
/// <remarks>
/// PATTERN: Scaffolded entities are schema-pure POCOs. 
/// Create hand-written partial classes that implement these interfaces when needed for app layer.
/// Example: public partial class Atom : IEntity&lt;long&gt;
/// </remarks>
public interface IEntity
{
}

/// <summary>
/// Base entity interface with strongly-typed primary key.
/// Enables generic operations without knowing concrete type.
/// </summary>
/// <typeparam name="TKey">Primary key type (int, long, Guid, etc.)</typeparam>
/// <remarks>
/// USE CASE: Pass IEntity&lt;long&gt; to service methods instead of full Atom with navigation graph.
/// Prevents loading unnecessary related entities, improves performance.
/// </remarks>
public interface IEntity<TKey> : IEntity where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Primary key for the entity.
    /// </summary>
    TKey Id { get; set; }
}
