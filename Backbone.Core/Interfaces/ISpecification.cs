// Backbone.Core/Interfaces/ISpecification.cs

using Backbone.Core.Entities;
using System.Linq.Expressions;

namespace Backbone.Core.Interfaces
{

    public interface ISpecification<T> where T : BaseEntity
    {
        // The WHERE condition (e.g., x => x.Id == id)
        Expression<Func<T, bool>> Criteria { get; }

        // List of includes (eager loading)
        List<Expression<Func<T, object>>> Includes { get; }

        // Sorting
        Expression<Func<T, object>> OrderBy { get; }
        Expression<Func<T, object>> OrderByDescending { get; }

        // Pagination
        int Take { get; }
        int Skip { get; }
        bool IsPagingEnabled { get; }
    }
}
