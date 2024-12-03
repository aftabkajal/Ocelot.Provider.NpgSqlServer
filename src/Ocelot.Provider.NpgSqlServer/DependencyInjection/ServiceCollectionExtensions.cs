using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ocelot.Configuration.Repository;
using Ocelot.DependencyInjection;
using Ocelot.Provider.NpgSqlServer.Configuration;
using Ocelot.Provider.NpgSqlServer.DatabaseContext;
using Ocelot.Provider.NpgSqlServer.Repository;

namespace Ocelot.Provider.NpgSqlServer.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IOcelotBuilder AddNpgSqlServerProvider(this IOcelotBuilder builder, Action<AppSettings> option)
        {
            builder.Services.Configure(option);

            builder.Services.AddSingleton(
                resolver => resolver.GetRequiredService<IOptions<AppSettings>>().Value);

            builder.Services.AddSingleton<IFileConfigurationRepository, NpgSqlServerFileConfigurationRepository>();

            builder.Services.AddDbContext<ApplicationDbContext>(ServiceLifetime.Singleton);

            return builder;
        }
    }
}
