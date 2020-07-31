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
            return status switch
            {
                MicrosoftHealthStatus.Healthy => HealthStatus.UP,
                MicrosoftHealthStatus.Degraded => HealthStatus.WARNING,
                MicrosoftHealthStatus.Unhealthy => HealthStatus.DOWN,
                _ => HealthStatus.UNKNOWN,
            };
        }

        public static MicrosoftHealthStatus ToHealthStatus(this HealthStatus status)
        {
            return status switch
            {
                HealthStatus.UP => MicrosoftHealthStatus.Healthy,
                HealthStatus.WARNING => MicrosoftHealthStatus.Degraded,
                _ => MicrosoftHealthStatus.Unhealthy,
            };
        }

        public static Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult ToHealthCheckResult(this HealthCheckResult result)
        {
            return new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult(result.Status.ToHealthStatus(), result.Description, null, result.Details);
        }

        public static HealthCheckResult ToHealthCheckResult(this Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult result)
        {
            return new Common.HealthChecks.HealthCheckResult()
            {
                Status = result.Status.ToHealthStatus(),
                Description = result.Description,
                Details = result.Data.ToDictionary(t => t.Key, t => t.Value)
            };
        }

        public static async Task<HealthCheckResult> HealthCheck(this HealthCheckRegistration registration, IServiceProvider provider)
        {
            var context = new HealthCheckContext { Registration = registration };
            var healthCheckResult = new HealthCheckResult();
            try
            {
                var res = await registration.Factory(provider).CheckHealthAsync(context).ConfigureAwait(false);
                healthCheckResult = new HealthCheckResult()
                {
                    Status = res.Status.ToHealthStatus(),
                    Description = res.Description,
                    Details = res.Data?.ToDictionary(i => i.Key, i => i.Value)
                };

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