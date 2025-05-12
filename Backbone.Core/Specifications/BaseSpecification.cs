// Core/Specifications/BaseSpecification.cs
using Backbone.Core.Entities;
using System.Linq.Expressions;

namespace Backbone.Core.Specifications
{
    public abstract class BaseSpecification<T> : ISpecification<T> where T : BaseEntity
    {
        protected BaseSpecification()
        {
        }

        protected BaseSpecification(Expression<Func<T, bool>> criteria)
        {
            Criteria = criteria;
        }

        public Expression<Func<T, bool>> Criteria { get; protected set; }
        public List<Expression<Func<T, object>>> Includes { get; } = new();
        public List<string> IncludeStrings { get; } = new();
        public Expression<Func<T, object>> OrderBy { get; private set; }
        public Expression<Func<T, object>> OrderByDescending { get; private set; }
        public int Take { get; private set; }
        public int Skip { get; private set; }
        public bool IsPagingEnabled { get; private set; }
        public bool IncludeDeleted { get; private set; }

        protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
        {
            Includes.Add(includeExpression);
        }

        protected virtual void AddInclude(string includeString)
        {
            IncludeStrings.Add(includeString);
        }

        protected virtual void ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
            IsPagingEnabled = true;
        }

        protected virtual void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
        {
            OrderBy = orderByExpression;
        }

        protected virtual void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescExpression)
        {
            OrderByDescending = orderByDescExpression;
        }

        protected virtual void IncludeDeletedItems()
        {
            IncludeDeleted = true;
        }

        public List<Expression<Func<T, object>>> OrderBys { get; } = new();
        public List<Expression<Func<T, object>>> OrderByDescendings { get; } = new();

        // Add methods to add ordering
        protected void AddOrderBy(Expression<Func<T, object>> orderByExpression)
        {
            OrderBys.Add(orderByExpression);
        }

        protected void AddOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
        {
            OrderByDescendings.Add(orderByDescendingExpression);
        }
    }
}