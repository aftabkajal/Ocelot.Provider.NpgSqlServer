using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Ocelot.Provider.NpgSqlServer.Configuration;
using Ocelot.Provider.NpgSqlServer.Models;

namespace Ocelot.Provider.NpgSqlServer.DatabaseContext
{
    public class ApplicationDbContext : DbContext
    {
        private readonly AppSettings _appSettings;
        private readonly string _jsonRouteSample = @"{
        'DownstreamPathTemplate': '/{everything}',
        'DownstreamScheme': 'http',
        'DownstreamHostAndPorts': [
            {
                'Host': 'localhost',
                'Port': 5095
            }
        ],
        'UpstreamPathTemplate': '/gateway/{everything}',
        'UpstreamHttpMethod': [
            'Get'
        ]}";

        public ApplicationDbContext(IOptions<AppSettings> options)
        {
            _appSettings = options.Value;
        }

        public DbSet<OcelotGlobalConfiguration> OcelotGlobalConfigurations { get; set; }

        public DbSet<OcelotRoute> OcelotRoutes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(_appSettings.DbConnectionStrings,
                b => b.MigrationsAssembly(_appSettings.MigrationsAssembly));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OcelotGlobalConfiguration>().HasData(new OcelotGlobalConfiguration { Id = 1, GatewayName = "TestGateway" });

            var json = JObject.Parse(_jsonRouteSample);
            modelBuilder.Entity<OcelotRoute>().HasData(new OcelotRoute { Id = 1, Route = json.ToString() });
        }
    }
}
