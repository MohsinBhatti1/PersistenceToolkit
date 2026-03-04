using PersistenceToolkit.Abstractions;
using PersistenceToolkit.Abstractions.Repositories;
using PersistenceToolkit.Domain;
using PersistenceToolkit.Persistence.Persistence;

namespace PersistenceToolkit.Repositories
{
    public class AggregateRepository<T> : EntityRepository<T>, IAggregateRepository<T> where T : Entity, IAggregateRoot
    {
        public AggregateRepository(BaseContext dbContext, ISystemUser systemUser) : base(dbContext, systemUser)
        {
        }
    }
}