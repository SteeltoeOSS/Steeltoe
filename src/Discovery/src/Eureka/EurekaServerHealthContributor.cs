// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Util;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// Reports whether the Eureka server is reachable.
/// </summary>
internal sealed class EurekaServerHealthContributor : IHealthContributor
{
    private readonly EurekaDiscoveryClient _discoveryClient;
    private readonly IOptionsMonitor<EurekaClientOptions> _clientOptionsMonitor;
    private readonly IOptionsMonitor<EurekaInstanceOptions> _instanceOptionsMonitor;

    public string Id => "eurekaServer";

    public EurekaServerHealthContributor(EurekaDiscoveryClient discoveryClient, IOptionsMonitor<EurekaClientOptions> clientOptionsMonitor,
        IOptionsMonitor<EurekaInstanceOptions> instanceOptionsMonitor)
    {
        ArgumentGuard.NotNull(discoveryClient);
        ArgumentGuard.NotNull(clientOptionsMonitor);
        ArgumentGuard.NotNull(instanceOptionsMonitor);

        _discoveryClient = discoveryClient;
        _clientOptionsMonitor = clientOptionsMonitor;
        _instanceOptionsMonitor = instanceOptionsMonitor;
    }

    public Task<HealthCheckResult?> CheckHealthAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        EurekaClientOptions clientOptions = _clientOptionsMonitor.CurrentValue;

        if (!clientOptions.Enabled || !clientOptions.Health.ContributorEnabled)
        {
            return Task.FromResult<HealthCheckResult?>(null);
        }

        var result = new HealthCheckResult();
        AddHealthStatus(result);
        AddApplications(_discoveryClient.Applications, result);
        return Task.FromResult<HealthCheckResult?>(result);
    }

    private void AddHealthStatus(HealthCheckResult result)
    {
        HealthStatus remoteStatus = AddRemoteInstanceStatus(result);
        HealthStatus fetchStatus = AddFetchStatus(result, _discoveryClient.LastGoodRegistryFetchTimeUtc);
        HealthStatus heartbeatStatus = AddHeartbeatStatus(result, _discoveryClient.LastGoodHeartbeatTimeUtc);

        result.Status = remoteStatus;

        if (fetchStatus > result.Status)
        {
            result.Status = fetchStatus;
        }

        if (heartbeatStatus > result.Status)
        {
            result.Status = heartbeatStatus;
        }

        result.Details.Add("status", result.Status.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
    }

    private HealthStatus AddRemoteInstanceStatus(HealthCheckResult result)
    {
        HealthStatus remoteStatus = MakeHealthStatus(_discoveryClient.LastRemoteInstanceStatus);
        result.Details.Add("remoteInstStatus", remoteStatus.ToString());
        return remoteStatus;
    }

    internal HealthStatus AddHeartbeatStatus(HealthCheckResult result, DateTime? lastGoodHeartbeatTimeUtc)
    {
        EurekaClientOptions clientOptions = _clientOptionsMonitor.CurrentValue;
        EurekaInstanceOptions instanceOptions = _instanceOptionsMonitor.CurrentValue;

        if (clientOptions is { ShouldRegisterWithEureka: true })
        {
            TimeSpan? elapsed = GetElapsedSince(lastGoodHeartbeatTimeUtc);

            if (elapsed == null)
            {
                result.Details.Add("heartbeat", "Not yet successfully connected");
                result.Details.Add("heartbeatStatus", HealthStatus.Unknown.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
                result.Details.Add("heartbeatTime", "UNKNOWN");
                return HealthStatus.Unknown;
            }

            TimeSpan renewalInternal = instanceOptions.LeaseRenewalInterval;

            if (elapsed > renewalInternal * 2)
            {
                result.Details.Add("heartbeat", "Reporting failures connecting");
                result.Details.Add("heartbeatStatus", HealthStatus.Down.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
                result.Details.Add("heartbeatTime", lastGoodHeartbeatTimeUtc.GetValueOrDefault().ToString("s", CultureInfo.InvariantCulture));
                result.Details.Add("heartbeatFailures", (long)(elapsed / renewalInternal));
                return HealthStatus.Down;
            }

            result.Details.Add("heartbeat", "Successful");
            result.Details.Add("heartbeatStatus", HealthStatus.Up.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
            result.Details.Add("heartbeatTime", lastGoodHeartbeatTimeUtc.GetValueOrDefault().ToString("s", CultureInfo.InvariantCulture));
            return HealthStatus.Up;
        }

        result.Details.Add("heartbeatStatus", "Not registering");
        return HealthStatus.Unknown;
    }

    internal HealthStatus AddFetchStatus(HealthCheckResult result, DateTime? lastGoodRegistryFetchTimeUtc)
    {
        EurekaClientOptions clientOptions = _clientOptionsMonitor.CurrentValue;

        if (clientOptions.ShouldFetchRegistry)
        {
            TimeSpan? elapsed = GetElapsedSince(lastGoodRegistryFetchTimeUtc);

            if (elapsed == null)
            {
                result.Details.Add("fetch", "Not yet successfully connected");
                result.Details.Add("fetchStatus", HealthStatus.Unknown.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
                result.Details.Add("fetchTime", "UNKNOWN");
                return HealthStatus.Unknown;
            }

            TimeSpan fetchInternal = clientOptions.RegistryFetchInterval;

            if (elapsed > fetchInternal * 2)
            {
                result.Details.Add("fetch", "Reporting failures connecting");
                result.Details.Add("fetchStatus", HealthStatus.Down.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
                result.Details.Add("fetchTime", lastGoodRegistryFetchTimeUtc.GetValueOrDefault().ToString("s", CultureInfo.InvariantCulture));
                result.Details.Add("fetchFailures", (long)(elapsed / fetchInternal));
                return HealthStatus.Down;
            }

            result.Details.Add("fetch", "Successful");
            result.Details.Add("fetchStatus", HealthStatus.Up.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
            result.Details.Add("fetchTime", lastGoodRegistryFetchTimeUtc.GetValueOrDefault().ToString("s", CultureInfo.InvariantCulture));
            return HealthStatus.Up;
        }

        result.Details.Add("fetchStatus", "Not fetching");
        return HealthStatus.Unknown;
    }

    internal HealthStatus MakeHealthStatus(InstanceStatus lastRemoteInstanceStatus)
    {
        return lastRemoteInstanceStatus switch
        {
            InstanceStatus.Down => HealthStatus.Down,
            InstanceStatus.OutOfService => HealthStatus.OutOfService,
            InstanceStatus.Up => HealthStatus.Up,
            _ => HealthStatus.Unknown
        };
    }

    internal void AddApplications(ApplicationInfoCollection applications, HealthCheckResult result)
    {
        var apps = new Dictionary<string, int>();

        foreach (ApplicationInfo app in applications.RegisteredApplications)
        {
            int instanceCount = app.Instances.Count;

            if (instanceCount > 0)
            {
                apps.Add(app.Name, instanceCount);
            }
        }

        if (apps.Count > 0)
        {
            result.Details.Add("applications", apps);
        }
        else
        {
            result.Details.Add("applications", "NONE");
        }
    }

    private static TimeSpan? GetElapsedSince(DateTime? dateTimeUtc)
    {
        return dateTimeUtc == null ? null : DateTime.UtcNow - dateTimeUtc.Value;
    }
}
