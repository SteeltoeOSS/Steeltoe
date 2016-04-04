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
//

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.OptionsModel;
using System;
using System.Collections.Generic;
using Microsoft.AspNet.Hosting;

namespace SteelToe.Discovery.Client
{
    internal class DiscoveryClientFactory
    {
        private static object _lock = new object();
        internal static object _discoveryClient;
        internal static object CreateDiscoveryClient(IServiceProvider provider)
        {
            if (_discoveryClient == null)
            {
                if (provider == null)
                {
                    return null;
                }

                lock (_lock)
                {
                    if (_discoveryClient == null)
                    {
                        var options = provider.GetService(typeof(IOptions<DiscoveryOptions>)) as IOptions<DiscoveryOptions>;
                        var logFactory = provider.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
                        var lifeCycle = provider.GetService(typeof(IApplicationLifetime)) as IApplicationLifetime;
                        _discoveryClient = CreateClient(options?.Value, lifeCycle, logFactory);
                    }
                }
            }

            return _discoveryClient;
        }

        internal static object CreateClient(DiscoveryOptions options, IApplicationLifetime lifeCycle = null, ILoggerFactory logFactory = null)
        {
            var logger = logFactory?.CreateLogger<DiscoveryClientFactory>();
            if (options == null)
            {
                logger?.LogWarning("Failed to create DiscoveryClient, no DiscoveryOptions");
                return _unknown;
            }

            if (options.ClientType == DiscoveryClientType.EUREKA)
            {
                var clientOpts = options.ClientOptions as EurekaClientOptions;
                if (clientOpts == null)
                {
                    logger?.LogWarning("Failed to create DiscoveryClient, no EurekaClientOptions");
                    return _unknown;
                }

                var instOpts = options.RegistrationOptions as EurekaInstanceOptions;
                return new EurekaDiscoveryClient(clientOpts, instOpts, lifeCycle, logFactory);
            } else
            {
                logger?.LogWarning("Failed to create DiscoveryClient, unknown ClientType: {0}", options.ClientType.ToString());
            }


            return _unknown;
        }
        private static UnknownDiscoveryClient _unknown = new UnknownDiscoveryClient();
    }
    public class UnknownDiscoveryClient : IDiscoveryClient
    {
        public string Description
        {
            get
            {
                return "Unknown";
            }
        }

        public IList<string> Services
        {
            get
            {
                return new List<string>();
            }
        }

        public IList<IServiceInstance> GetInstances(string serviceId)
        {
            return new List<IServiceInstance>();
        }

        public IServiceInstance GetLocalServiceInstance()
        {
            return null;
        }

        public void ShutdownAsync()
        {
            
        }
    }
}
