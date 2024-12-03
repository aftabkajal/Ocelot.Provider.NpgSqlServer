using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.Repository;
using Ocelot.Configuration.Setter;
using Ocelot.DependencyInjection;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Responses;
using System.Diagnostics;

namespace Ocelot.Provider.NpgSqlServer.Middleware
{
    public static class OcelotMiddlewareExtensions
    {
        public static async Task<IApplicationBuilder> UseOcelotWithNpgSqlServerProvider(this IApplicationBuilder builder)
        {
            await builder.UseOcelotWithNpgSqlServerProvider(new OcelotPipelineConfiguration());
            return builder;
        }

        public static async Task<IApplicationBuilder> UseOcelotWithNpgSqlServerProvider(this IApplicationBuilder builder, OcelotPipelineConfiguration pipelineConfiguration)
        {
            var configuration = await CreateConfiguration(builder);

            ConfigureDiagnosticListener(builder);

            return CreateOcelotPipeline(builder, pipelineConfiguration);
        }

        private static async Task<IInternalConfiguration> CreateConfiguration(IApplicationBuilder builder)
        {
            var fileConfig = await builder.ApplicationServices.GetService<IFileConfigurationRepository>().Get();

            // now create the config
            var internalConfigCreator = builder.ApplicationServices.GetService<IInternalConfigurationCreator>();
            var internalConfig = await internalConfigCreator.Create(fileConfig.Data);

            //Configuration error, throw error message
            if (internalConfig.IsError)
            {
                ThrowToStopOcelotStarting(internalConfig);
            }

            // now save it in memory
            var internalConfigRepo = builder.ApplicationServices.GetService<IInternalConfigurationRepository>();
            internalConfigRepo.AddOrReplace(internalConfig.Data);

            var adminPath = builder.ApplicationServices.GetService<IAdministrationPath>();

            var configurations = builder.ApplicationServices.GetServices<OcelotMiddlewareConfigurationDelegate>();

            // Todo - this has just been added for consul so far...will there be an ordering problem in the future? Should refactor all config into this pattern?
            foreach (var configuration in configurations)
            {
                await configuration(builder);
            }

            if (AdministrationApiInUse(adminPath))
            {
                //We have to make sure the file config is set for the ocelot.env.json and ocelot.json so that if we pull it from the 
                //admin api it works...boy this is getting a spit spags boll.
                var fileConfigSetter = builder.ApplicationServices.GetService<IFileConfigurationSetter>();

                //  await SetFileConfig(fileConfigSetter, fileConfig.Data);
            }

            return GetOcelotConfigAndReturn(internalConfigRepo);
        }

        private static bool AdministrationApiInUse(IAdministrationPath adminPath)
        {
            return adminPath != null;
        }

        private static bool IsError(Response response)
        {
            return response == null || response.IsError;
        }

        private static IInternalConfiguration GetOcelotConfigAndReturn(IInternalConfigurationRepository provider)
        {
            var ocelotConfiguration = provider.Get();

            if (ocelotConfiguration?.Data == null || ocelotConfiguration.IsError)
            {
                ThrowToStopOcelotStarting(ocelotConfiguration);
            }

            return ocelotConfiguration.Data;
        }

        private static void ThrowToStopOcelotStarting(Response config)
        {
            throw new Exception($"Unable to start Ocelot, errors are: {string.Join(",", config.Errors.Select(x => x.ToString()))}");
        }

        private static IApplicationBuilder CreateOcelotPipeline(IApplicationBuilder builder, OcelotPipelineConfiguration pipelineConfiguration)
        {
            builder.BuildOcelotPipeline(pipelineConfiguration);
            builder.Properties["analysis.NextMiddlewareName"] = "TransitionToOcelotMiddleware";
            return builder;
        }

        private static void ConfigureDiagnosticListener(IApplicationBuilder builder)
        {
            var listener = builder.ApplicationServices.GetService<OcelotDiagnosticListener>();
            var diagnosticListener = builder.ApplicationServices.GetService<DiagnosticListener>();
            diagnosticListener.SubscribeWithAdapter(listener);
        }
    }
}
