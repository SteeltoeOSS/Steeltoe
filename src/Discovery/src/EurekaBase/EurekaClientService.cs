// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Transport;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Steeltoe.Discovery.Eureka
{
    public static class EurekaClientService
    {
        /// <summary>
        /// Using the Eureka configuration values provided in <paramref name="configuration"/> contact the Eureka server and
        /// return all the service instances for the provided <paramref name="serviceId"/>. The Eureka client is shutdown after contacting the server.
        /// </summary>
        /// <param name="configuration">configuration values used for configuring the Eureka client</param>
        /// <param name="serviceId">the Eureka service id to look up all instances of</param>
        /// <param name="logFactory">optional log factory to use for logging</param>
        /// <returns>service instances</returns>
        public static IList<IServiceInstance> GetInstances(IConfiguration configuration, string serviceId, ILoggerFactory logFactory = null)
        {
            EurekaClientOptions config = ConfigureClientOptions(configuration);
            LookupClient client = GetLookupClient(config, logFactory);
            var result = client.GetInstancesInternal(serviceId);
            client.ShutdownAsync().GetAwaiter().GetResult();
            return result;
        }

        /// <summary>
        /// Using the Eureka configuration values provided in <paramref name="configuration"/> contact the Eureka server and
        /// return all the registered services. The Eureka client is shutdown after contacting the server.
        /// </summary>
        /// <param name="configuration">configuration values used for configuring the Eureka client</param>
        /// <param name="logFactory">optional log factory to use for logging</param>
        /// <returns>all registered services</returns>
        public static IList<string> GetServices(IConfiguration configuration, ILoggerFactory logFactory = null)
        {
            EurekaClientOptions config = ConfigureClientOptions(configuration);
            var client = GetLookupClient(config, logFactory);
            var result = client.GetServicesInternal();
            client.ShutdownAsync().GetAwaiter().GetResult();
            return result;
        }

        internal static LookupClient GetLookupClient(EurekaClientOptions config, ILoggerFactory logFactory)
        {
            return new LookupClient(config, null, logFactory);
        }

        internal static EurekaClientOptions ConfigureClientOptions(IConfiguration configuration)
        {
            var clientConfigsection = configuration.GetSection(EurekaClientOptions.EUREKA_CLIENT_CONFIGURATION_PREFIX);

            var clientOptions = new EurekaClientOptions();
            clientConfigsection.Bind(clientOptions);
            clientOptions.ShouldFetchRegistry = true;
            clientOptions.ShouldRegisterWithEureka = false;
            return clientOptions;
        }

        internal class LookupClient : DiscoveryClient
        {
            public LookupClient(IEurekaClientConfig clientConfig, IEurekaHttpClient httpClient = null, ILoggerFactory logFactory = null)
                : base(clientConfig, httpClient, logFactory)
            {
                if (_cacheRefreshTimer != null)
                {
                    _cacheRefreshTimer.Dispose();
                    _cacheRefreshTimer = null;
                }
            }

            public IList<IServiceInstance> GetInstancesInternal(string serviceId)
            {
                IList<InstanceInfo> infos = GetInstancesByVipAddress(serviceId, false);
                List<IServiceInstance> instances = new List<IServiceInstance>();
                foreach (InstanceInfo info in infos)
                {
                    _logger?.LogDebug($"GetInstances returning: {info}");
                    instances.Add(new EurekaServiceInstance(info));
                }

                return instances;
            }

            public IList<string> GetServicesInternal()
            {
                Applications applications = Applications;
                if (applications == null)
                {
                    return new List<string>();
                }

                IList<Application> registered = applications.GetRegisteredApplications();
                List<string> names = new List<string>();
                foreach (Application app in registered)
                {
                    if (app.Instances.Count == 0)
                    {
                        continue;
                    }

                    names.Add(app.Name.ToLowerInvariant());
                }

                return names;
            }
        }
    }
}
