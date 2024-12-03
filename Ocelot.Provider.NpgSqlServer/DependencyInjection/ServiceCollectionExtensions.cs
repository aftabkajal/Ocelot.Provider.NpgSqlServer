using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ocelot.Configuration.Repository;
using Ocelot.DependencyInjection;
using Ocelot.Provider.NpgSqlServer.Configuration;
using Ocelot.Provider.NpgSqlServer.Db;
using Ocelot.Provider.NpgSqlServer.Repository;

namespace Ocelot.Provider.NpgSqlServer.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IOcelotBuilder AddNpgSqlServerProvider(this IOcelotBuilder builder, Action<AppConfigs> option)
        {
            builder.Services.Configure(option);

            builder.Services.AddSingleton(
                resolver => resolver.GetRequiredService<IOptions<AppConfigs>>().Value);

            builder.Services.AddSingleton<IFileConfigurationRepository, NpgSqlServerFileConfigurationRepository>();

            builder.Services.AddDbContext<AppDbContext>(ServiceLifetime.Singleton);

            return builder;
        }
    }
}
