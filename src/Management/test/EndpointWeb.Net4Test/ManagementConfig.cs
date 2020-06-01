// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            ActuatorConfigurator.ClearManagementOptions();

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
