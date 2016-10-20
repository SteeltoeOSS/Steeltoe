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
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;

namespace Steeltoe.Discovery.Client
{
    public class DiscoveryClientFactory
    {
   
        protected DiscoveryOptions _config;

        internal DiscoveryClientFactory()
        {

        }

        public DiscoveryClientFactory(DiscoveryOptions config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            _config = config;
        }

        internal protected virtual object Create(IServiceProvider provider)
        {
            if (provider == null)
            {
                return null;
            }

            ConfigureOptions();

            var logFactory = provider.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
            var lifeCycle = provider.GetService(typeof(IApplicationLifetime)) as IApplicationLifetime;
   
            return CreateClient(lifeCycle, logFactory);
        }

        public virtual object CreateClient(IApplicationLifetime lifeCycle = null, ILoggerFactory logFactory = null)
        {
            var logger = logFactory?.CreateLogger<DiscoveryClientFactory>();
            if (_config == null)
            {
                logger?.LogWarning("Failed to create DiscoveryClient, no DiscoveryOptions");
                return _unknown;
            }

            if (_config.ClientType == DiscoveryClientType.EUREKA)
            {
                var clientOpts = _config.ClientOptions as EurekaClientOptions;
                if (clientOpts == null)
                {
                    logger?.LogWarning("Failed to create DiscoveryClient, no EurekaClientOptions");
                    return _unknown;
                }

                var instOpts = _config.RegistrationOptions as EurekaInstanceOptions;
                return new EurekaDiscoveryClient(clientOpts, instOpts, lifeCycle, logFactory);
            } else
            {
                logger?.LogWarning("Failed to create DiscoveryClient, unknown ClientType: {0}", _config.ClientType.ToString());
            }

            return _unknown;
        }
        internal protected virtual void ConfigureOptions()
        {

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
