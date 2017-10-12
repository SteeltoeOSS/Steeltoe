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
using Steeltoe.Common.Discovery;
using System;


namespace Steeltoe.Common.Http
{
    public class DiscoveryHttpClientHandler : DiscoveryHttpClientHandlerBase
    {
        private IDiscoveryClient _client;
        private ILogger<DiscoveryHttpClientHandler> _logger;

        public DiscoveryHttpClientHandler(IDiscoveryClient client, ILogger<DiscoveryHttpClientHandler> logger = null) : base()
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _client = client;
            _logger = logger;
        }

        public override Uri LookupService(Uri current)
        {
            _logger?.LogDebug("LookupService({0})", current.ToString());
            if (!current.IsDefaultPort)
            {
                return current;
            }

            var instances = _client.GetInstances(current.Host);
            if (instances.Count > 0)
            {
                int indx = _random.Next(instances.Count);
                current = new Uri(instances[indx].Uri, current.PathAndQuery);
            }
            _logger?.LogDebug("LookupService() returning {0} ", current.ToString());
            return current;

        }
    }
}


