using PersistenceToolkit.Abstractions.Specifications;
using PersistenceToolkit.Tests.Entities;

namespace PersistenceToolkit.Tests.Specifications
{
    public class ParentSpec : EntitySpecification<Parent>
    {
        public ParentSpec()
        {
            Query.Include(c => c.Children)
                 .ThenInclude(child => child.GrandChildren);
        }
    }
} 