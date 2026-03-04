using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersistenceToolkit.Abstractions;
using PersistenceToolkit.Abstractions.Repositories;
using PersistenceToolkit.Persistence;
using PersistenceToolkit.Repositories;
using PersistenceToolkit.Tests.DBContext;
using System;
using PersistenceToolkit.Persistence.Persistence;

namespace PersistenceToolkit.Tests.Initializers
{
    internal static class DependencyInjectionSetup
    {
        internal static ServiceProvider InitializeServiceProvider()
        {
            var services = new ServiceCollection();

            RegisterDependncies(services);
            return services.BuildServiceProvider();
        }

        private static void RegisterDependncies(ServiceCollection services)
        {
            services.AddScoped<ISystemUser>(serviceProvider =>
            {
                return new SystemUser { TenantId = 1, UserId = 1 };
            });
            services.AddScoped<BaseContext>(serviceProvider =>
            {
                return CreateInMemoryContext();
            });

            services.AddPersistenceToolkit();
        }

        private static SystemContext CreateInMemoryContext()
        {
            var dbName = $"TestDB_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<BaseContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new SystemContext(options);
        }
        private static SystemContext CreateDBContext()
        {
            return new SystemContext("Data Source=MOHSIN-PC\\SQLEXPRESS;Initial Catalog=TEST;Integrated Security=True;TrustServerCertificate=True;");
        }
    }
}
