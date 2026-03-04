using Microsoft.Extensions.DependencyInjection;
using PersistenceToolkit.Abstractions;
using PersistenceToolkit.Abstractions.Repositories;
using PersistenceToolkit.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistenceToolkit.Persistence
{
    public static class PersistenceToolkitServiceCollectionExtensions
    {
        public static IServiceCollection AddPersistenceToolkit(this IServiceCollection services)
        {
            services.AddScoped(typeof(IGenericReadRepository<>), typeof(GenericRepository<>));
            services.AddScoped(typeof(IEntityReadRepository<>), typeof(EntityRepository<>));
            services.AddScoped(typeof(IAggregateReadRepository<>), typeof(AggregateRepository<>));
            services.AddScoped(typeof(IAggregateRepository<>), typeof(AggregateRepository<>));
            return services;
        }
    }
}
