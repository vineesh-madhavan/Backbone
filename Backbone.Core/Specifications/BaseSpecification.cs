﻿// Core/Specifications/BaseSpecification.cs
using System.Linq.Expressions;

namespace Backbone.Core.Specifications
{
    public abstract class BaseSpecification<T> : ISpecification<T>
    {
        protected BaseSpecification() { }
        protected BaseSpecification(Expression<Func<T, bool>> criteria) => Criteria = criteria;

        public Expression<Func<T, bool>>? Criteria { get; }
        public List<Expression<Func<T, object>>> Includes { get; } = new();
        public List<string> IncludeStrings { get; } = new(); // Added
        public Expression<Func<T, object>>? OrderBy { get; private set; }
        public Expression<Func<T, object>>? OrderByDescending { get; private set; }
        public List<Expression<Func<T, object>>>? OrderBys { get; private set; } // Added
        public List<Expression<Func<T, object>>>? OrderBysDescending { get; private set; } // Added
        public int Take { get; private set; }
        public int Skip { get; private set; }
        public bool IsPagingEnabled { get; private set; }

        protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
            => Includes.Add(includeExpression);

        protected virtual void AddInclude(string includeString)
            => IncludeStrings.Add(includeString);

        protected virtual void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
            => OrderBy = orderByExpression;

        protected virtual void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
            => OrderByDescending = orderByDescendingExpression;

        protected virtual void ApplyOrderBys(params Expression<Func<T, object>>[] orderByExpressions)
            => OrderBys = orderByExpressions.ToList();

        protected virtual void ApplyOrderBysDescending(params Expression<Func<T, object>>[] orderByDescendingExpressions)
            => OrderBysDescending = orderByDescendingExpressions.ToList();

        protected virtual void ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
            IsPagingEnabled = true;
        }
    }
}