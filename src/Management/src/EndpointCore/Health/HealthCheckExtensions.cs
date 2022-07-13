// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HealthCheckResult = Steeltoe.Common.HealthChecks.HealthCheckResult;
using HealthStatus = Steeltoe.Common.HealthChecks.HealthStatus;
using MicrosoftHealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;
using Steeltoe.Common.Util;

namespace Steeltoe.Management.Endpoint.Health;

public static class HealthCheckExtensions
{
    public static HealthStatus ToHealthStatus(this MicrosoftHealthStatus status)
    {
        return status switch
        {
            MicrosoftHealthStatus.Healthy => HealthStatus.Up,
            MicrosoftHealthStatus.Degraded => HealthStatus.Warning,
            MicrosoftHealthStatus.Unhealthy => HealthStatus.Down,
            _ => HealthStatus.Unknown,
        };
    }

    public static async Task<HealthCheckResult> HealthCheck(this HealthCheckRegistration registration, IServiceProvider provider)
    {
        var context = new HealthCheckContext { Registration = registration };
        var healthCheckResult = new HealthCheckResult();
        try
        {
            var res = await registration.Factory(provider).CheckHealthAsync(context).ConfigureAwait(false);
            var status = res.Status.ToHealthStatus();
            healthCheckResult.Status = status; // Only used for aggregate doesn't get reported
            healthCheckResult.Description = res.Description;
            healthCheckResult.Details = new Dictionary<string, object>(res.Data)
            {
                { "status", status.ToSnakeCaseString(SnakeCaseStyle.AllCaps) },
                { "description", res.Description }
            };

            if (res.Exception != null && !string.IsNullOrEmpty(res.Exception.Message))
            {
                healthCheckResult.Details.Add("error", res.Exception.Message);
            }
        }
        catch (Exception e)
        {
            // Catch all exceptions so that a status can always be returned
            healthCheckResult.Details.Add("exception", e.Message);
        }

        return healthCheckResult;
    }
}
