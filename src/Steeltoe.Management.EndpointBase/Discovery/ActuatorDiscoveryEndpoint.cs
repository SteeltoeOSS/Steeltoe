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
using Steeltoe.Management.Endpoint.CloudFoundry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Discovery
{
    public class ActuatorDiscoveryEndpoint : AbstractEndpoint<Links, string>
    {
        private ILogger<ActuatorDiscoveryEndpoint> _logger;
        private IManagementOptions _mgmtOption;

        public ActuatorDiscoveryEndpoint(IActuatorDiscoveryOptions options, IEnumerable<IManagementOptions> mgmtOptions, ILogger<ActuatorDiscoveryEndpoint> logger = null)
        : base(options)
        {
            _mgmtOption = mgmtOptions?.OfType<ActuatorManagementOptions>().Single();
            _logger = logger;
        }

        protected new IActuatorDiscoveryOptions Options => options as IActuatorDiscoveryOptions;

        public override Links Invoke(string baseUrl)
        {
            DiscoveryService discoveryService = new DiscoveryService(_mgmtOption, options, _logger);
            return discoveryService.Invoke(baseUrl);
        }
    }
}
