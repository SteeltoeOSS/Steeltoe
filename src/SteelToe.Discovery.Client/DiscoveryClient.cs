//
// Copyright 2015 the original author or authors.
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
//

using System.Collections.Generic;
using Microsoft.Extensions.OptionsModel;
using Microsoft.Extensions.Logging;

namespace SteelToe.Discovery.Client
{
    public class DiscoveryClient : IDiscoveryClient
    {

        internal IDiscoveryClient ClientDelegate { get; set; }

        private ILogger _logger;

        public DiscoveryClient(IOptions<DiscoveryOptions> options, ILoggerFactory logFactory = null)
        {
            _logger = logFactory?.CreateLogger<DiscoveryClient>();
            if (options == null)
            {
                _logger?.LogWarning("Failed to create IDiscoveryClient, no options");
                return;
            }
            ClientDelegate = CreateClientDelegate(options.Value, logFactory);
            if (ClientDelegate == null)
            {
                _logger?.LogWarning("Failed to create IDiscoveryClient: {0}", options.Value.ClientType);
            }

        }

        public string Description
        {
            get
            {
                if (ClientDelegate != null)
                {
                    return ClientDelegate.Description;
                }
                return "Unknown";
            }
        }

        public IList<string> Services
        {
            get
            {
                if (ClientDelegate != null)
                {
                    return ClientDelegate.Services;
                }
                return new List<string>();
            }
        }

        public IList<IServiceInstance> GetInstances(string serviceId)
        {
            if (ClientDelegate != null)
            {
                return ClientDelegate.GetInstances(serviceId);
            }
            return new List<IServiceInstance>();
        }

        public IServiceInstance GetLocalServiceInstance()
        {
            if (ClientDelegate != null)
            {
                return ClientDelegate.GetLocalServiceInstance();
            }
            return null;
        }

        public virtual IDiscoveryClient CreateClientDelegate(DiscoveryOptions options, ILoggerFactory logFactory = null)
        {
            if (options == null)
            {
                return null;
            }

            if (options.ClientType == DiscoveryClientType.EUREKA)
            {
                var clientOpts = options.ClientOptions as EurekaClientOptions;
                if (clientOpts == null)
                {
                    return null;
                }

                var instOpts = options.RegistrationOptions as EurekaInstanceOptions;
                return new EurekaDiscoveryClient(clientOpts, instOpts, logFactory);
            }

            return null;
        }
    }
}
