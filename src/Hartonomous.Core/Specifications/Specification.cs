using System;
using System.Linq.Expressions;

namespace Hartonomous.Core.Specifications;

/// <summary>
/// Specification pattern for encapsulating query logic.
/// Provides reusable, composable query predicates with compile-time type safety.
/// </summary>
/// <typeparam name="T">The entity type to query</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// The criteria expression for filtering entities.
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>
    /// Expressions for including related entities.
    /// </summary>
    List<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// String-based include expressions for complex paths.
    /// </summary>
    List<string> IncludeStrings { get; }

    /// <summary>
    /// Order by expression (ascending).
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Order by expression (descending).
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Number of items to skip (for paging).
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Number of items to take (for paging).
    /// </summary>
    int? Take { get; }
}

/// <summary>
/// Base implementation of specification pattern with fluent API.
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public abstract class Specification<T> : ISpecification<T>
{
    public Expression<Func<T, bool>>? Criteria { get; private set; }
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public int? Skip { get; private set; }
    public int? Take { get; private set; }

    /// <summary>
    /// Set the filter criteria.
    /// </summary>
    protected void AddCriteria(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    /// <summary>
    /// Include a related entity.
    /// </summary>
    protected void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    /// <summary>
    /// Include using string path (for nested includes).
    /// </summary>
    protected void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    /// <summary>
    /// Set ascending order.
    /// </summary>
    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    /// <summary>
    /// Set descending order.
    /// </summary>
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        OrderByDescending = orderByDescendingExpression;
    }

    /// <summary>
    /// Apply paging.
    /// </summary>
    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }
}

/// <summary>
/// Specification combinator for AND logic.
/// </summary>
public class AndSpecification<T> : Specification<T>
{
    public AndSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        if (left.Criteria != null && right.Criteria != null)
        {
            var parameter = Expression.Parameter(typeof(T));
            var leftVisitor = new ReplaceExpressionVisitor(left.Criteria.Parameters[0], parameter);
            var rightVisitor = new ReplaceExpressionVisitor(right.Criteria.Parameters[0], parameter);

            var leftBody = leftVisitor.Visit(left.Criteria.Body);
            var rightBody = rightVisitor.Visit(right.Criteria.Body);

            var combined = Expression.AndAlso(leftBody!, rightBody!);
            AddCriteria(Expression.Lambda<Func<T, bool>>(combined, parameter));
        }
    }

    private class ReplaceExpressionVisitor : ExpressionVisitor
    {
        private readonly Expression _oldValue;
        private readonly Expression _newValue;

        public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public override Expression? Visit(Expression? node)
        {
            return node == _oldValue ? _newValue : base.Visit(node);
        }
    }
}

/// <summary>
/// Specification combinator for OR logic.
/// </summary>
public class OrSpecification<T> : Specification<T>
{
    public OrSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        if (left.Criteria != null && right.Criteria != null)
        {
            var parameter = Expression.Parameter(typeof(T));
            var leftVisitor = new ReplaceExpressionVisitor(left.Criteria.Parameters[0], parameter);
            var rightVisitor = new ReplaceExpressionVisitor(right.Criteria.Parameters[0], parameter);

            var leftBody = leftVisitor.Visit(left.Criteria.Body);
            var rightBody = rightVisitor.Visit(right.Criteria.Body);

            var combined = Expression.OrElse(leftBody!, rightBody!);
            AddCriteria(Expression.Lambda<Func<T, bool>>(combined, parameter));
        }
    }

    private class ReplaceExpressionVisitor : ExpressionVisitor
    {
        private readonly Expression _oldValue;
        private readonly Expression _newValue;

        public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public override Expression? Visit(Expression? node)
        {
            return node == _oldValue ? _newValue : base.Visit(node);
        }
    }
}

/// <summary>
/// Extension methods for combining specifications.
/// </summary>
public static class SpecificationExtensions
{
    public static ISpecification<T> And<T>(this ISpecification<T> left, ISpecification<T> right)
    {
        return new AndSpecification<T>(left, right);
    }

    public static ISpecification<T> Or<T>(this ISpecification<T> left, ISpecification<T> right)
    {
        return new OrSpecification<T>(left, right);
    }
}
