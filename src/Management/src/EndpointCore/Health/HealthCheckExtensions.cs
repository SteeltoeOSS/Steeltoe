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

        public static MicrosoftHealthStatus ToHealthStatus(this HealthStatus status)
        {
            switch (status)
            {
                case HealthStatus.UP: return MicrosoftHealthStatus.Healthy;
                case HealthStatus.WARNING: return MicrosoftHealthStatus.Degraded;
                default: return MicrosoftHealthStatus.Unhealthy;
            }
        }

        public static Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult ToHealthCheckResult(this HealthCheckResult result)
        {
            return new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult(result.Status.ToHealthStatus(), result.Description, null, result.Details);
        }

        public static HealthCheckResult ToHealthCheckResult(this Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult result)
        {
            return new HealthCheckResult()
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