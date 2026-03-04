using PersistenceToolkit.Abstractions.Specifications;
using PersistenceToolkit.Domain;

namespace PersistenceToolkit.Abstractions.Specifications
{
    public class GetByIdSpecification<T> : EntitySpecification<T>
        where T : Entity
    {
        public GetByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id);
        }
    }
}