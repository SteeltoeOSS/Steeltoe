// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
            var result = client.GetInstances(serviceId);
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
            var result = client.GetServices();
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

            public IList<IServiceInstance> GetInstances(string serviceId)
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

            public IList<string> GetServices()
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
