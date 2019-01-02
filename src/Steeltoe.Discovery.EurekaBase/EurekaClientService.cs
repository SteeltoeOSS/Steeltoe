// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Transport;
using System.Collections.Generic;
using System.Net.Http;

namespace Steeltoe.Discovery.Eureka
{
    public static class EurekaClientService
    {
        public static IList<IServiceInstance> GetInstances(string eurekaServerUri, string serviceId, HttpClient httpClient = null, ILoggerFactory logFactory = null)
        {
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = true,
                ShouldRegisterWithEureka = false,
                EurekaServerServiceUrls = eurekaServerUri
            };
            LookupClient client = GetLookupClient(config, httpClient, logFactory);
            return client.GetInstances(serviceId);
        }

        public static IList<string> GetServices(string eurekaServerUri, HttpClient httpClient = null, ILoggerFactory logFactory = null)
        {
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = true,
                ShouldRegisterWithEureka = false,
                EurekaServerServiceUrls = eurekaServerUri
            };
            var client = GetLookupClient(config, httpClient, logFactory);
            return client.GetServices();
        }

        private static LookupClient GetLookupClient(EurekaClientConfig config, HttpClient httpClient, ILoggerFactory logFactory)
        {
            if (httpClient == null)
            {
                return new LookupClient(config, null, logFactory);
            }
            else
            {
                var eurekaHttpClient = new EurekaHttpClient(config, httpClient, logFactory);
                return new LookupClient(config, eurekaHttpClient, logFactory);
            }
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
