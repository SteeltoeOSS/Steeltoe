// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Steeltoe.Common.Util;
using SteeltoeHealthCheckResult = Steeltoe.Common.HealthChecks.HealthCheckResult;
using SteeltoeHealthStatus = Steeltoe.Common.HealthChecks.HealthStatus;

namespace Steeltoe.Management.Endpoint.Health;

public static class HealthCheckExtensions
{
    public static SteeltoeHealthStatus ToHealthStatus(this HealthStatus status)
    {
        return status switch
        {
            HealthStatus.Healthy => SteeltoeHealthStatus.Up,
            HealthStatus.Degraded => SteeltoeHealthStatus.Warning,
            HealthStatus.Unhealthy => SteeltoeHealthStatus.Down,
            _ => SteeltoeHealthStatus.Unknown
        };
    }

    public static async Task<SteeltoeHealthCheckResult> HealthCheckAsync(this HealthCheckRegistration registration, IServiceProvider provider)
    {
        var context = new HealthCheckContext
        {
            Registration = registration
        };

        var healthCheckResult = new SteeltoeHealthCheckResult();

        try
        {
            HealthCheckResult res = await registration.Factory(provider).CheckHealthAsync(context);

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
