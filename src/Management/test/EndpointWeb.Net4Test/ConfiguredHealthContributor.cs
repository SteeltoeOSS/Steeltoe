// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Management.EndpointWeb.Test
{
    internal class ConfiguredHealthContributor : IHealthContributor
    {
        private IConfiguration _config;

        public string Id => "config-based";

        public ConfiguredHealthContributor(IConfiguration config)
        {
            _config = config;
        }

        public HealthCheckResult Health()
        {
            var health = new HealthCheckResult();
            if (_config.GetValue<bool>("unhealthy"))
            {
                health.Status = HealthStatus.DOWN;
            }
            else
            {
                health.Status = HealthStatus.UP;
            }

            return health;
        }
    }
}
