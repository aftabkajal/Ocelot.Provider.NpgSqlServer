﻿using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Ocelot.Cache;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Provider.NpgSqlServer.Configuration;
using Ocelot.Provider.NpgSqlServer.Db;
using Ocelot.Provider.NpgSqlServer.Extensions;
using Ocelot.Responses;

namespace Ocelot.Provider.NpgSqlServer.Repository
{
    public class NpgSqlServerFileConfigurationRepository : IFileConfigurationRepository
    {
        private readonly IOcelotCache<FileConfiguration> _cache;
        private readonly AppDbContext _dbContext;
        private readonly AppConfigs _option;

        public NpgSqlServerFileConfigurationRepository(AppConfigs option,
                                                    IOcelotCache<FileConfiguration> cache,
                                                    AppDbContext dbContext)
        {
            _option = option;
            _cache = cache;
            _dbContext = dbContext;
        }

        public Task<Response> Set(FileConfiguration fileConfiguration)
        {
            _cache.AddAndDelete(_option.CachePrefix + "FileConfiguration", fileConfiguration, TimeSpan.FromSeconds(1800), "");
            return Task.FromResult((Response)new OkResponse());
        }

        public async Task<Response<FileConfiguration>> Get()
        {
            var config = _cache.Get(_option.CachePrefix + "FileConfiguration", "");

            if (config != null)
            {
                return new OkResponse<FileConfiguration>(config);
            }

            var file = new FileConfiguration();

            var result = await _dbContext.OcelotGlobalConfigurations.AsNoTracking().FirstOrDefaultAsync();
            if (result != null)
            {
                var glb = new FileGlobalConfiguration
                {
                    BaseUrl = result.BaseUrl,
                    DownstreamScheme = result.DownstreamScheme,
                    RequestIdKey = result.RequestIdKey
                };

                if (!string.IsNullOrEmpty(result.HttpHandlerOptions))
                {
                    glb.HttpHandlerOptions = result.HttpHandlerOptions.ToObject<FileHttpHandlerOptions>();
                }
                if (!string.IsNullOrEmpty(result.LoadBalancerOptions))
                {
                    glb.LoadBalancerOptions = result.LoadBalancerOptions.ToObject<FileLoadBalancerOptions>();
                }
                if (!string.IsNullOrEmpty(result.QoSOptions))
                {
                    glb.QoSOptions = result.QoSOptions.ToObject<FileQoSOptions>();
                }
                if (!string.IsNullOrEmpty(result.ServiceDiscoveryProvider))
                {
                    glb.ServiceDiscoveryProvider = result.ServiceDiscoveryProvider.ToObject<FileServiceDiscoveryProvider>();
                }
                file.GlobalConfiguration = glb;

                try
                {
                    var ocelotRoutes = await _dbContext.OcelotRoutes.AsNoTracking().ToListAsync();

                    if (ocelotRoutes != null && ocelotRoutes.Any())
                    {
                        var routeList = new List<FileRoute>();
                        foreach (var model in ocelotRoutes)
                        {
                            var m = new FileRoute();
                            var fileRoute = JsonConvert.DeserializeObject<FileRoute>(model.Route);

                            if (fileRoute is null)
                            {
                                continue;
                            }

                            if (fileRoute.AddHeadersToRequest != null)
                            {
                                m.AddHeadersToRequest = fileRoute.AddHeadersToRequest;
                            }

                            if (fileRoute.AuthenticationOptions != null)
                            {
                                m.AuthenticationOptions = fileRoute.AuthenticationOptions;
                            }

                            //if (!String.IsNullOrEmpty(fileRoute.CacheOptions))
                            //{
                            //    m.FileCacheOptions = model.CacheOptions.ToObject<FileCacheOptions>();
                            //}

                            if (fileRoute.DelegatingHandlers != null)
                            {
                                m.DelegatingHandlers = fileRoute.DelegatingHandlers;
                            }

                            if (fileRoute.LoadBalancerOptions != null)
                            {
                                m.LoadBalancerOptions = fileRoute.LoadBalancerOptions;
                            }

                            if (fileRoute.QoSOptions != null)
                            {
                                m.QoSOptions = fileRoute.QoSOptions;
                            }

                            if (fileRoute.DownstreamHostAndPorts != null)
                            {
                                m.DownstreamHostAndPorts = fileRoute.DownstreamHostAndPorts;
                            }

                            m.DownstreamPathTemplate = fileRoute.DownstreamPathTemplate;
                            m.DownstreamScheme = fileRoute.DownstreamScheme;
                            m.Key = fileRoute.Key;
                            m.Priority = fileRoute.Priority;
                            m.RequestIdKey = fileRoute.RequestIdKey;
                            m.ServiceName = fileRoute.ServiceName;
                            m.Timeout = fileRoute.Timeout;
                            m.UpstreamHost = fileRoute.UpstreamHost;

                            if (fileRoute.UpstreamHttpMethod != null)
                            {
                                m.UpstreamHttpMethod = fileRoute.UpstreamHttpMethod;
                            }

                            m.UpstreamPathTemplate = fileRoute.UpstreamPathTemplate;
                            routeList.Add(m);
                        }
                        file.Routes = routeList;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }
            else
            {
                throw new Exception("Exception occurred in SqlServerFileConfigurationRepository");
            }

            if (file.Routes == null || file.Routes.Count == 0)
            {
                return new OkResponse<FileConfiguration>(new());
            }
            return new OkResponse<FileConfiguration>(file);
        }
    }
}
