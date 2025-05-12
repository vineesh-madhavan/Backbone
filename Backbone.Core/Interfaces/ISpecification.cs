// Backbone.Core/Interfaces/ISpecification.cs

using System.Linq.Expressions;

public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    List<string> IncludeStrings { get; }
    Expression<Func<T, object>> OrderBy { get; }
    Expression<Func<T, object>> OrderByDescending { get; }
    List<Expression<Func<T, object>>> OrderBys { get; } // Add this
    List<Expression<Func<T, object>>> OrderByDescendings { get; } // Optional for multiple descending
    int Take { get; }
    int Skip { get; }
    bool IsPagingEnabled { get; }
    bool IncludeDeleted { get; }
}
