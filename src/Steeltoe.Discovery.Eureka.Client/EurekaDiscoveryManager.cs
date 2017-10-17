//
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
//

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace Steeltoe.Discovery.Eureka
{
    public class EurekaDiscoveryManager : DiscoveryManager
    {
        private IOptionsMonitor<EurekaClientOptions> _clientConfig;
        private IOptionsMonitor<EurekaInstanceOptions> _instConfig;

        // Constructor used via Dependency Injection
        public EurekaDiscoveryManager(
            IOptionsMonitor<EurekaClientOptions> clientConfig,
            IOptionsMonitor<EurekaInstanceOptions> instConfig,
            EurekaDiscoveryClient client,
            ILoggerFactory logFactory = null)
        {
            _logger = logFactory?.CreateLogger<DiscoveryManager>();
            _clientConfig = clientConfig;
            _instConfig = instConfig;
            Client = client;
        }

        public override IEurekaClientConfig ClientConfig
        {
            get
            {
                return _clientConfig.CurrentValue;
            }
        }

        public override IEurekaInstanceConfig InstanceConfig
        {
            get
            {
                return _instConfig.CurrentValue;
            }
        }
    }
}
