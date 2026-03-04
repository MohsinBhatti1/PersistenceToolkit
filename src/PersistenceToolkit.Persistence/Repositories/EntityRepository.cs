using Microsoft.EntityFrameworkCore;
using PersistenceToolkit.Abstractions;
using PersistenceToolkit.Abstractions.Repositories;
using PersistenceToolkit.Abstractions.Specifications;
using PersistenceToolkit.Domain;
using PersistenceToolkit.Persistence.Persistence;

namespace PersistenceToolkit.Repositories
{
    public class EntityRepository<T> : GenericRepository<T>, IEntityReadRepository<T> where T : Entity
    {
        private readonly ISystemUser _systemUser;
        public EntityRepository(BaseContext dbContext, ISystemUser systemUser) : base(dbContext)
        {
            _systemUser = systemUser;
        }

        #region Specification Methods
        protected override IQueryable<T> ApplySpecification(ISpecification<T> specification, bool evaluateCriteriaOnly = false)
        {
            if (specification is EntitySpecification<T> baseSpec)
                ApplyTenantAndSoftDeleteFilters(baseSpec);

            return base.ApplySpecification(specification, evaluateCriteriaOnly);
        }

        protected override IQueryable<TResult> ApplySpecification<TResult>(ISpecification<T, TResult> specification)
        {
            if (specification is EntitySpecification<T, TResult> baseSpec)
                ApplyTenantAndSoftDeleteFilters(baseSpec);

            return base.ApplySpecification(specification);
        }

        private void ApplyTenantAndSoftDeleteFilters(EntitySpecification<T> specification)
        {
            var context = (BaseContext)DbContext;

            if (!context.IsPropertyIgnored<T>(c => c.TenantId) &&
                !specification.IgnoreCompanyFilter)
            {
                specification.Query.Where(c => c.TenantId == _systemUser.TenantId);
            }

            if (!context.IsPropertyIgnored<T>(c => c.IsDeleted) &&
                !specification.IncludeDeletedRecords)
            {
                specification.Query.Where(c => !c.IsDeleted);
            }
        }

        private void ApplyTenantAndSoftDeleteFilters<TResult>(EntitySpecification<T, TResult> specification)
        {
            var context = (BaseContext)DbContext;

            if (!context.IsPropertyIgnored<T>(c => c.TenantId) &&
                !specification.IgnoreCompanyFilter)
            {
                specification.Query.Where(c => c.TenantId == _systemUser.TenantId);
            }

            if (!context.IsPropertyIgnored<T>(c => c.IsDeleted) &&
                !specification.IncludeDeletedRecords)
            {
                specification.Query.Where(c => !c.IsDeleted);
            }
        }

        public async Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var spec = new GetByIdSpecification<T>(id);
            return await SingleOrDefaultAsync(spec, cancellationToken);
        }

        #endregion

        #region Save(Add & Update)/Delete Methods
        public override async Task<bool> Save(T entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            SetAuditLogs(entity);

            var result = await base.Save(entity, cancellationToken);

            if (result)
                ResetHasChanges(entity);

            return result;
        }

        private void ResetHasChanges(T entity)
        {
            AggregateWalker.TraverseEntities(entity, item =>
            {
                item.CaptureLoadTimeSnapshot();
            });
        }
        private void SetAuditLogs(T entity)
        {
            AggregateWalker.TraverseEntities(entity, item =>
            {
                if (item.HasChange())
                {
                    // Only set TenantId for new entities (Id == 0)
                    if (item.Id == 0)
                    {
                        item.SetTenantId(_systemUser.TenantId);
                    }
                    item.SetAuditLogs(_systemUser.UserId, DateTime.Now);
                }
            });
        }
        private void SetAuditLogs(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                SetAuditLogs(entity);
            }
        }
        private void ResetHasChanges(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                ResetHasChanges(entity);
            }
        }

        public async override Task<bool> SaveRange(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            SetAuditLogs(entities);
            var result = await base.SaveRange(entities, cancellationToken);

            if (result)
                ResetHasChanges(entities);

            return result;
        }
        public override async Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            entity.MarkAsDeleted(_systemUser.UserId, DateTime.Now);

            DbContext.Set<T>().Attach(entity);
            DbContext.Entry(entity).Property(a => a.IsDeleted).IsModified = true;
            DbContext.Entry(entity).Property(a => a.DeletedOn).IsModified = true;
            DbContext.Entry(entity).Property(a => a.DeletedBy).IsModified = true;

            return await SaveChangesAsync(cancellationToken) > 0;
        }
        public override async Task<bool> DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            foreach (var entity in entities)
            {
                entity.MarkAsDeleted(_systemUser.UserId, DateTime.Now);

                DbContext.Set<T>().Attach(entity);
                DbContext.Entry(entity).Property(a => a.IsDeleted).IsModified = true;
                DbContext.Entry(entity).Property(a => a.DeletedOn).IsModified = true;
                DbContext.Entry(entity).Property(a => a.DeletedBy).IsModified = true;
            }
            return await SaveChangesAsync(cancellationToken) > 0;
        }
        public override async Task<bool> DeleteRangeAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
        {
            var entities = ApplySpecification(specification).ToList();
            foreach (var entity in entities)
            {
                entity.MarkAsDeleted(_systemUser.UserId, DateTime.Now);

                DbContext.Set<T>().Attach(entity);
                DbContext.Entry(entity).Property(a => a.IsDeleted).IsModified = true;
                DbContext.Entry(entity).Property(a => a.DeletedOn).IsModified = true;
                DbContext.Entry(entity).Property(a => a.DeletedBy).IsModified = true;
            }
            return await SaveChangesAsync(cancellationToken) > 0;
        }
        #endregion

    }
}
