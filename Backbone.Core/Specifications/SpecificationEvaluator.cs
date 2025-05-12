using Backbone.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backbone.Core.Specifications
{
    public static class SpecificationEvaluator<T> where T : BaseEntity
    {
        public static IQueryable<T> GetQuery(
            IQueryable<T> inputQuery,
            ISpecification<T> specification)
        {
            var query = inputQuery;

            // Apply the criteria (WHERE clause)
            if (specification.Criteria != null)
            {
                query = query.Where(specification.Criteria);
            }

            // Apply soft delete filter unless explicitly included
            if (!specification.IncludeDeleted)
            {
                query = query.Where(x => !x.IsDeleted);
            }

            // Apply includes (eager loading)
            query = specification.Includes.Aggregate(
                query,
                (current, include) => current.Include(include));

            // Apply string-based includes
            query = specification.IncludeStrings.Aggregate(
                query,
                (current, include) => current.Include(include));

            // Apply ordering
            if (specification.OrderBy != null)
            {
                query = query.OrderBy(specification.OrderBy);
            }
            else if (specification.OrderByDescending != null)
            {
                query = query.OrderByDescending(specification.OrderByDescending);
            }

            // Apply paging
            if (specification.IsPagingEnabled)
            {
                query = query.Skip(specification.Skip)
                             .Take(specification.Take);
            }

            return query;
        }
    }
}
