using System.Linq.Expressions;

namespace Hartonomous.Core.Data;

/// <summary>
/// Specification pattern interface for encapsulating query logic.
/// Enables composable, reusable, and testable query specifications.
/// </summary>
/// <typeparam name="TEntity">The entity type this specification applies to.</typeparam>
public interface ISpecification<TEntity> where TEntity : class
{
    /// <summary>
    /// Gets the filter criteria for the query.
    /// </summary>
    Expression<Func<TEntity, bool>>? Criteria { get; }

    /// <summary>
    /// Gets the collection of include expressions for eager loading.
    /// </summary>
    IReadOnlyList<Expression<Func<TEntity, object>>> Includes { get; }

    /// <summary>
    /// Gets the collection of include expressions using string paths.
    /// </summary>
    IReadOnlyList<string> IncludeStrings { get; }

    /// <summary>
    /// Gets the collection of order-by expressions.
    /// </summary>
    IReadOnlyList<(Expression<Func<TEntity, object>> KeySelector, bool Ascending)> OrderBy { get; }

    /// <summary>
    /// Gets the number of records to skip (for pagination).
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Gets the number of records to take (for pagination).
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Gets a value indicating whether to track entities in the query.
    /// </summary>
    bool AsNoTracking { get; }

    /// <summary>
    /// Gets a value indicating whether to ignore query filters (e.g., soft delete filters).
    /// </summary>
    bool IgnoreQueryFilters { get; }

    /// <summary>
    /// Gets a value indicating whether to split the query for better performance.
    /// </summary>
    bool AsSplitQuery { get; }
}

/// <summary>
/// Base abstract class for implementing specifications with fluent API.
/// </summary>
/// <typeparam name="TEntity">The entity type this specification applies to.</typeparam>
public abstract class Specification<TEntity> : ISpecification<TEntity> where TEntity : class
{
    private readonly List<Expression<Func<TEntity, object>>> _includes = new();
    private readonly List<string> _includeStrings = new();
    private readonly List<(Expression<Func<TEntity, object>> KeySelector, bool Ascending)> _orderBy = new();

    /// <inheritdoc />
    public Expression<Func<TEntity, bool>>? Criteria { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<Expression<Func<TEntity, object>>> Includes => _includes.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyList<string> IncludeStrings => _includeStrings.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyList<(Expression<Func<TEntity, object>> KeySelector, bool Ascending)> OrderBy => _orderBy.AsReadOnly();

    /// <inheritdoc />
    public int? Skip { get; private set; }

    /// <inheritdoc />
    public int? Take { get; private set; }

    /// <inheritdoc />
    public bool AsNoTracking { get; private set; }

    /// <inheritdoc />
    public bool IgnoreQueryFilters { get; private set; }

    /// <inheritdoc />
    public bool AsSplitQuery { get; private set; }

    /// <summary>
    /// Adds a filter criteria to the specification.
    /// </summary>
    /// <param name="criteria">The filter expression.</param>
    protected void AddCriteria(Expression<Func<TEntity, bool>> criteria)
    {
        Criteria = criteria;
    }

    /// <summary>
    /// Adds an include expression for eager loading.
    /// </summary>
    /// <param name="includeExpression">The include expression.</param>
    protected void AddInclude(Expression<Func<TEntity, object>> includeExpression)
    {
        _includes.Add(includeExpression);
    }

    /// <summary>
    /// Adds an include expression using a string path.
    /// </summary>
    /// <param name="includeString">The include path.</param>
    protected void AddInclude(string includeString)
    {
        _includeStrings.Add(includeString);
    }

    /// <summary>
    /// Adds an ascending order-by expression.
    /// </summary>
    /// <param name="orderByExpression">The order-by expression.</param>
    protected void AddOrderBy(Expression<Func<TEntity, object>> orderByExpression)
    {
        _orderBy.Add((orderByExpression, true));
    }

    /// <summary>
    /// Adds a descending order-by expression.
    /// </summary>
    /// <param name="orderByDescExpression">The order-by expression.</param>
    protected void AddOrderByDescending(Expression<Func<TEntity, object>> orderByDescExpression)
    {
        _orderBy.Add((orderByDescExpression, false));
    }

    /// <summary>
    /// Applies pagination to the specification.
    /// </summary>
    /// <param name="skip">The number of records to skip.</param>
    /// <param name="take">The number of records to take.</param>
    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }

    /// <summary>
    /// Configures the query to not track entities.
    /// </summary>
    protected void ApplyNoTracking()
    {
        AsNoTracking = true;
    }

    /// <summary>
    /// Configures the query to ignore global query filters.
    /// </summary>
    protected void ApplyIgnoreQueryFilters()
    {
        IgnoreQueryFilters = true;
    }

    /// <summary>
    /// Configures the query to use split query execution.
    /// </summary>
    protected void ApplySplitQuery()
    {
        AsSplitQuery = true;
    }
}

/// <summary>
/// Composite specification that combines multiple specifications using AND logic.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public class AndSpecification<TEntity> : Specification<TEntity> where TEntity : class
{
    public AndSpecification(ISpecification<TEntity> left, ISpecification<TEntity> right)
    {
        if (left.Criteria != null && right.Criteria != null)
        {
            var parameter = Expression.Parameter(typeof(TEntity));
            var leftVisitor = new ReplaceParameterVisitor(left.Criteria.Parameters[0], parameter);
            var leftExpression = leftVisitor.Visit(left.Criteria.Body);

            var rightVisitor = new ReplaceParameterVisitor(right.Criteria.Parameters[0], parameter);
            var rightExpression = rightVisitor.Visit(right.Criteria.Body);

            var combined = Expression.AndAlso(leftExpression!, rightExpression!);
            AddCriteria(Expression.Lambda<Func<TEntity, bool>>(combined, parameter));
        }
    }

    private class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ReplaceParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }
}
