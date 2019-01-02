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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Handler;
using Steeltoe.Management.Endpoint.Health.Contributor;
using System.Collections.Generic;

namespace Steeltoe.Management.EndpointWeb.Test
{
    public class ManagementConfig
    {
        public static IList<IActuatorHandler> ConfigureManagementActuators(LoggerFactory loggerFactory = null, Dictionary<string, string> settings = null)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            ActuatorConfigurator.ConfiguredHandlers.Clear(); // Clear setup between tests

            ActuatorConfigurator.UseAllActuators(configuration, null, GetHealthContributors(configuration), null, loggerFactory);
            return ActuatorConfigurator.ConfiguredHandlers;
        }

        private static IEnumerable<IHealthContributor> GetHealthContributors(IConfiguration configuration)
        {
            var healthContributors = new List<IHealthContributor> { new DiskSpaceContributor(), };

            return healthContributors;
        }
    }
}
