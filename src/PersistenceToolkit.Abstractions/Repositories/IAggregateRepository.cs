using PersistenceToolkit.Abstractions.Specifications;
using PersistenceToolkit.Domain;

namespace PersistenceToolkit.Abstractions.Repositories
{
    public interface IAggregateReadRepository<T> : IEntityReadRepository<T> where T : Entity, IAggregateRoot
    {
    }
    public interface IAggregateRepository<T> : IAggregateReadRepository<T> where T : Entity, IAggregateRoot
    {
        Task<bool> Save(T entity, CancellationToken cancellationToken = default);
        Task<bool> SaveRange(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken = default);
        Task<bool> DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        Task<bool> DeleteRangeAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
    }
}
