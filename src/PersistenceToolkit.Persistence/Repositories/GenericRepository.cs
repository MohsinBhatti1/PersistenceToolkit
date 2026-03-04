using Microsoft.EntityFrameworkCore;
using PersistenceToolkit.Abstractions.Repositories;
using PersistenceToolkit.Abstractions.Specifications;
using PersistenceToolkit.Domain;
using PersistenceToolkit.Persistence.Persistence;
using System.Data.SqlTypes;
using System.Linq;

namespace PersistenceToolkit.Repositories
{
    public class GenericRepository<T> : IGenericReadRepository<T> where T : class
    {
        protected DbContext DbContext { get; set; }
        protected ISpecificationEvaluator SpecificationEvaluator { get; set; }

        private readonly EntityStateProcessor _entityStateProcessor;
        public GenericRepository(BaseContext dbContext)
            : this(dbContext, Persistence.SpecificationEvaluators.SpecificationEvaluator.Default)
        {
            _entityStateProcessor = new EntityStateProcessor(dbContext);
        }
        public GenericRepository(DbContext dbContext, ISpecificationEvaluator specificationEvaluator)
        {
            DbContext = dbContext;
            SpecificationEvaluator = specificationEvaluator;
        }

        public virtual async Task<bool> Save(T entity, CancellationToken cancellationToken = default)
        {
            _entityStateProcessor.SetState(entity);
            return await SaveChangesAsync(cancellationToken) > 0;
        }
        public virtual async Task<bool> SaveRange(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            foreach (var entity in entities)
                _entityStateProcessor.SetState(entity);

            return await SaveChangesAsync(cancellationToken) > 0;
        }
        public virtual new async Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            DbContext.Set<T>().Remove(entity);
            return await SaveChangesAsync(cancellationToken) > 0;
        }
        public virtual new async Task<bool> DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            DbContext.Set<T>().RemoveRange(entities);
            return await SaveChangesAsync(cancellationToken) > 0;
        }
        public virtual new async Task<bool> DeleteRangeAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            var query = ApplySpecification(specification);
            DbContext.Set<T>().RemoveRange(query);

            return await SaveChangesAsync(cancellationToken) > 0;
        }
        public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var result = await DbContext.SaveChangesAsync(cancellationToken);
            _entityStateProcessor.DetachedAllTrackedEntries();
            return result;
        }

        protected virtual IQueryable<T> ApplySpecification(ISpecification<T> specification, bool evaluateCriteriaOnly = false)
        {
            return SpecificationEvaluator.GetQuery(DbContext.Set<T>().AsQueryable(), specification, evaluateCriteriaOnly).AsNoTracking();
        }
        protected virtual IQueryable<TResult> ApplySpecification<TResult>(ISpecification<T, TResult> specification)
        {
            return SpecificationEvaluator.GetQuery(DbContext.Set<T>().AsQueryable(), specification);
        }
        public virtual async Task<T> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            var queryResult = await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);

            if (queryResult == null) return null;

            return ResultWithPostProcessingSpecificationAction(specification, queryResult);
        }
        public virtual async Task<TResult?> FirstOrDefaultAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        {
            var queryResult = await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);

            if (queryResult == null) return default;

            return ResultWithPostProcessingSpecificationAction(specification, queryResult);
        }
        public virtual async Task<T> SingleOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            var queryResult = await ApplySpecification(specification).SingleOrDefaultAsync(cancellationToken);

            if (queryResult == null) return null;

            return ResultWithPostProcessingSpecificationAction(specification, queryResult);
        }
        public virtual async Task<TResult?> SingleOrDefaultAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        {
            var queryResult = await ApplySpecification(specification).SingleOrDefaultAsync(cancellationToken);

            if (queryResult == null) return default;

            return ResultWithPostProcessingSpecificationAction(specification, queryResult);
        }
        private T ResultWithPostProcessingSpecificationAction(ISpecification<T> specification, T queryResult)
        {
            return specification.PostProcessingAction is null
                ? queryResult
                : specification.PostProcessingAction(new List<T> { queryResult }).SingleOrDefault();
        }
        private TResult ResultWithPostProcessingSpecificationAction<TResult>(ISpecification<T, TResult> specification, TResult queryResult)
        {
            return specification.PostProcessingAction is null
                ? queryResult
                : specification.PostProcessingAction(new List<TResult> { queryResult }).SingleOrDefault();
        }

        #region List Methods with pagination
        public async Task<PaginatedResult<T>> PaginatedListAsync(ISpecification<T> specification, int skip, int take, CancellationToken cancellationToken = default)
        {
            SetPaginationValues(specification, skip, take);
            var result = await ListAsync(specification);
            RemovePaginationValues(specification);

            int count = 0;
            if (result.Count > 0 && skip == 0)
                count = await CountAsync(specification);
            return new PaginatedResult<T>(result, count);
        }
        public async Task<PaginatedResult<TResult>> PaginatedListAsync<TResult>(ISpecification<T, TResult> specification, int skip, int take, CancellationToken cancellationToken = default)
        {
            SetPaginationValues(specification, skip, take);
            var result = await ListAsync(specification);
            RemovePaginationValues(specification);

            int count = 0;
            if (result.Count > 0 && skip == 0)
                count = await CountAsync(specification);
            return new PaginatedResult<TResult>(result, count);
        }
        private static void SetPaginationValues(ISpecification<T> specification, int skip, int take)
        {
            specification.Query.Skip(skip);
            specification.Query.Take(take);
        }
        private static void RemovePaginationValues(ISpecification<T> specification)
        {
            specification.Query.Skip(-1);
            specification.Query.Take(-1);
        }

        public virtual async Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            var queryResult = await ApplySpecification(specification).ToListAsync(cancellationToken);

            return specification.PostProcessingAction is null
                ? queryResult
                : specification.PostProcessingAction(queryResult).AsList();
        }
        public virtual async Task<List<TResult>> ListAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
        {
            var queryResult = await ApplySpecification(specification).ToListAsync(cancellationToken);

            return specification.PostProcessingAction is null
                ? queryResult
                : specification.PostProcessingAction(queryResult).AsList();
        }
        public virtual async Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            return await ApplySpecification(specification, true).CountAsync(cancellationToken);
        }
        public virtual async Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            return await ApplySpecification(specification, true).AnyAsync(cancellationToken);
        }
        #endregion
    }
}
