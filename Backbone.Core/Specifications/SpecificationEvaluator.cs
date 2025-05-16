//Backbone.Core/Specifications/SpecificationEvaluator.cs
using Backbone.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Backbone.Core.Specifications
{
    public static class SpecificationEvaluator<T> where T : BaseEntity
    {
        public static IQueryable<T> GetQuery(
            IQueryable<T> inputQuery,
            ISpecification<T> specification,
            ILogger logger = null)
        {
            using var _ = logger?.BeginScope(new
            {
                Operation = "SpecificationEvaluation",
                EntityType = typeof(T).Name,
                SpecificationType = specification.GetType().Name
            });

            try
            {
                logger?.LogDebug("Starting specification evaluation for {EntityType}", typeof(T).Name);
                var stopwatch = Stopwatch.StartNew();

                var query = inputQuery;

                // Apply the criteria (WHERE clause)
                if (specification.Criteria != null)
                {
                    logger?.LogDebug("Applying criteria expression");
                    query = query.Where(specification.Criteria);
                }

                // Apply soft delete filter unless explicitly included
                if (!specification.IncludeDeleted)
                {
                    logger?.LogDebug("Applying soft delete filter");
                    query = query.Where(x => !x.IsDeleted);
                }

                // Apply includes (eager loading)
                if (specification.Includes.Any())
                {
                    logger?.LogDebug("Applying {IncludeCount} include expressions", specification.Includes.Count);
                    query = specification.Includes.Aggregate(
                        query,
                        (current, include) => current.Include(include));
                }

                // Apply string-based includes
                if (specification.IncludeStrings.Any())
                {
                    logger?.LogDebug("Applying {IncludeStringCount} string-based includes", specification.IncludeStrings.Count);
                    query = specification.IncludeStrings.Aggregate(
                        query,
                        (current, include) => current.Include(include));
                }

                // Apply ordering
                if (specification.OrderBy != null)
                {
                    logger?.LogDebug("Applying OrderBy expression");
                    query = query.OrderBy(specification.OrderBy);
                }
                else if (specification.OrderByDescending != null)
                {
                    logger?.LogDebug("Applying OrderByDescending expression");
                    query = query.OrderByDescending(specification.OrderByDescending);
                }

                // Apply paging
                if (specification.IsPagingEnabled)
                {
                    logger?.LogDebug("Applying paging (Skip: {Skip}, Take: {Take})",
                        specification.Skip, specification.Take);
                    query = query.Skip(specification.Skip)
                                .Take(specification.Take);
                }

                stopwatch.Stop();
                logger?.LogInformation("Successfully evaluated specification in {ElapsedMilliseconds}ms",
                    stopwatch.ElapsedMilliseconds);

                return query;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error evaluating specification for {EntityType}", typeof(T).Name);
                throw; // Re-throw to allow caller to handle
            }
        }
    }
}
