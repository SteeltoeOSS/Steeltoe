// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Linq;
using System.Threading.Tasks;
using HealthCheckResult = Steeltoe.Common.HealthChecks.HealthCheckResult;
using HealthStatus = Steeltoe.Common.HealthChecks.HealthStatus;
using MicrosoftHealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Steeltoe.Management.Endpoint.Health
{
    public static class HealthCheckExtensions
    {
        public static HealthStatus ToHealthStatus(this MicrosoftHealthStatus status)
        {
            switch (status)
            {
                case MicrosoftHealthStatus.Healthy: return HealthStatus.UP;
                case MicrosoftHealthStatus.Degraded: return HealthStatus.WARNING;
                case MicrosoftHealthStatus.Unhealthy: return HealthStatus.DOWN;
                default: return HealthStatus.UNKNOWN;
            }
        }

        public static async Task<HealthCheckResult> HealthCheck(this HealthCheckRegistration registration, IServiceProvider provider)
        {
            var context = new HealthCheckContext { Registration = registration };
            var healthCheckResult = new HealthCheckResult();
            try
            {
                var res = await registration.Factory(provider).CheckHealthAsync(context).ConfigureAwait(false);
                healthCheckResult.Status = res.Status.ToHealthStatus();
                healthCheckResult.Description = res.Description;
                healthCheckResult.Details = res.Data?.ToDictionary(i => i.Key, i => i.Value);

                if (res.Exception != null && !string.IsNullOrEmpty(res.Exception.Message))
                {
                    healthCheckResult.Details.Add("error", res.Exception.Message);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
            {
                // Catch all exceptions so that a status can always be returned
                healthCheckResult.Details.Add("exception", e.Message);
            }
#pragma warning restore CA1031 // Do not catch general exception types

            return healthCheckResult;
        }
    }
}