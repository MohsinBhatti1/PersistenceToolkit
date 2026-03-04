using Microsoft.Extensions.DependencyInjection;
using PersistenceToolkit.Abstractions;
using PersistenceToolkit.Abstractions.Repositories;
using PersistenceToolkit.Repositories;
using PersistenceToolkit.Tests.DBContext;
using PersistenceToolkit.Tests.Entities;

namespace PersistenceToolkit.Tests.Initializers
{
    public class PTKTestFixture : IDisposable
    {
        public IAggregateRepository<Parent> ParentTableRepository;
        public IAggregateRepository<User> UserRepository;
        public SystemContext SystemContext;
        public PTKTestFixture()
        {
            var serviceProvider = DependencyInjectionSetup.InitializeServiceProvider();

            SystemContext = serviceProvider.GetService<SystemContext>();
            var systemUser = serviceProvider.GetService<ISystemUser>();
            ParentTableRepository = serviceProvider.GetService<IAggregateRepository<Parent>>();
            UserRepository = serviceProvider.GetService<IAggregateRepository<User>>();
        }
        public void Dispose()
        {
            // Cleanup resources
        }
    }
}
