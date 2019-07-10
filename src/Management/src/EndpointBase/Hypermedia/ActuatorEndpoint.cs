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

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.CloudFoundry;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Hypermedia
{
    /// <summary>
    /// Actuator Endpoint provider the hypermedia link collection for all registered and enabled actuators
    /// </summary>
#pragma warning disable CS0612 // Type or member is obsolete
    public class ActuatorEndpoint : AbstractEndpoint<Links, string>
#pragma warning restore CS0612 // Type or member is obsolete
    {
        private readonly ILogger<ActuatorEndpoint> _logger;
        private readonly IManagementOptions _mgmtOption;

        public ActuatorEndpoint(IActuatorHypermediaOptions options, IEnumerable<IManagementOptions> mgmtOptions, ILogger<ActuatorEndpoint> logger = null)
        : base(options)
        {
            _mgmtOption = mgmtOptions?.OfType<ActuatorManagementOptions>().Single();
            _logger = logger;
        }

        protected new IActuatorHypermediaOptions Options => options as IActuatorHypermediaOptions;

#pragma warning disable CS0612 // Type or member is obsolete
        public override Links Invoke(string baseUrl)
#pragma warning restore CS0612 // Type or member is obsolete
        {
            HypermediaService service = new HypermediaService(_mgmtOption, options, _logger);
            return service.Invoke(baseUrl);
        }
    }
}
