// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Util;
using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka;

public class EurekaServerHealthContributor : IHealthContributor
{
    private readonly EurekaDiscoveryClient _discoveryClient;
    private readonly EurekaApplicationInfoManager _appInfoManager;

    public string Id => "eurekaServer";

    public EurekaServerHealthContributor(EurekaDiscoveryClient discoveryClient, EurekaApplicationInfoManager appInfoManager,
        ILogger<EurekaServerHealthContributor> logger = null)
    {
        _discoveryClient = discoveryClient;
        _appInfoManager = appInfoManager;
    }

    // Testing
    internal EurekaServerHealthContributor()
    {
    }

    public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = new HealthCheckResult();
        AddHealthStatus(result);
        AddApplications(_discoveryClient.Applications, result);
        return Task.FromResult(result);
    }

    private void AddHealthStatus(HealthCheckResult result)
    {
        HealthStatus remoteStatus = AddRemoteInstanceStatus(result);
        HealthStatus fetchStatus = AddFetchStatus(_discoveryClient.ClientConfiguration, result, _discoveryClient.LastGoodRegistryFetchTimestamp);

        HealthStatus heartBeatStatus = AddHeartbeatStatus(_discoveryClient.ClientConfiguration, _appInfoManager.InstanceConfig, result,
            _discoveryClient.LastGoodHeartbeatTimestamp);

        result.Status = remoteStatus;

        if (fetchStatus > result.Status)
        {
            result.Status = fetchStatus;
        }

        if (heartBeatStatus > result.Status)
        {
            result.Status = heartBeatStatus;
        }

        result.Details.Add("status", result.Status.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
    }

    private HealthStatus AddRemoteInstanceStatus(HealthCheckResult result)
    {
        HealthStatus remoteStatus = MakeHealthStatus(_discoveryClient.LastRemoteInstanceStatus);
        result.Details.Add("remoteInstStatus", remoteStatus.ToString());
        return remoteStatus;
    }

    internal HealthStatus AddHeartbeatStatus(IEurekaClientConfiguration clientConfiguration, IEurekaInstanceConfig instanceConfig, HealthCheckResult result,
        long lastGoodHeartbeatTimeTicks)
    {
        if (clientConfiguration != null && clientConfiguration.ShouldRegisterWithEureka)
        {
            long lastGoodHeartbeatPeriod = GetLastGoodHeartbeatTimePeriod(lastGoodHeartbeatTimeTicks);

            if (lastGoodHeartbeatPeriod <= 0)
            {
                result.Details.Add("heartbeat", "Not yet successfully connected");
                result.Details.Add("heartbeatStatus", HealthStatus.Unknown.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
                result.Details.Add("heartbeatTime", "UNKNOWN");
                return HealthStatus.Unknown;
            }

            if (lastGoodHeartbeatPeriod > instanceConfig.LeaseRenewalIntervalInSeconds * TimeSpan.TicksPerSecond * 2)
            {
                result.Details.Add("heartbeat", "Reporting failures connecting");
                result.Details.Add("heartbeatStatus", HealthStatus.Down.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
                result.Details.Add("heartbeatTime", new DateTime(lastGoodHeartbeatTimeTicks, DateTimeKind.Utc).ToString("s", CultureInfo.InvariantCulture));
                result.Details.Add("heartbeatFailures", lastGoodHeartbeatPeriod / (instanceConfig.LeaseRenewalIntervalInSeconds * TimeSpan.TicksPerSecond));
                return HealthStatus.Down;
            }

            result.Details.Add("heartbeat", "Successful");
            result.Details.Add("heartbeatStatus", HealthStatus.Up.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
            result.Details.Add("heartbeatTime", new DateTime(lastGoodHeartbeatTimeTicks, DateTimeKind.Utc).ToString("s", CultureInfo.InvariantCulture));
            return HealthStatus.Up;
        }

        result.Details.Add("heartbeatStatus", "Not registering");
        return HealthStatus.Unknown;
    }

    internal HealthStatus AddFetchStatus(IEurekaClientConfiguration clientConfiguration, HealthCheckResult result, long lastGoodFetchTimeTicks)
    {
        if (clientConfiguration != null && clientConfiguration.ShouldFetchRegistry)
        {
            long lastGoodFetchPeriod = GetLastGoodRegistryFetchTimePeriod(lastGoodFetchTimeTicks);

            if (lastGoodFetchPeriod <= 0)
            {
                result.Details.Add("fetch", "Not yet successfully connected");
                result.Details.Add("fetchStatus", HealthStatus.Unknown.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
                result.Details.Add("fetchTime", "UNKNOWN");
                return HealthStatus.Unknown;
            }

            if (lastGoodFetchPeriod > clientConfiguration.RegistryFetchIntervalSeconds * TimeSpan.TicksPerSecond * 2)
            {
                result.Details.Add("fetch", "Reporting failures connecting");
                result.Details.Add("fetchStatus", HealthStatus.Down.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
                result.Details.Add("fetchTime", new DateTime(lastGoodFetchTimeTicks, DateTimeKind.Utc).ToString("s", CultureInfo.InvariantCulture));
                result.Details.Add("fetchFailures", lastGoodFetchPeriod / (clientConfiguration.RegistryFetchIntervalSeconds * TimeSpan.TicksPerSecond));
                return HealthStatus.Down;
            }

            result.Details.Add("fetch", "Successful");
            result.Details.Add("fetchStatus", HealthStatus.Up.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
            result.Details.Add("fetchTime", new DateTime(lastGoodFetchTimeTicks, DateTimeKind.Utc).ToString("s", CultureInfo.InvariantCulture));
            return HealthStatus.Up;
        }

        result.Details.Add("fetchStatus", "Not fetching");
        return HealthStatus.Unknown;
    }

    internal HealthStatus MakeHealthStatus(InstanceStatus lastRemoteInstanceStatus)
    {
        if (lastRemoteInstanceStatus == InstanceStatus.Down)
        {
            return HealthStatus.Down;
        }

        if (lastRemoteInstanceStatus == InstanceStatus.OutOfService)
        {
            return HealthStatus.OutOfService;
        }

        if (lastRemoteInstanceStatus == InstanceStatus.Up)
        {
            return HealthStatus.Up;
        }

        return HealthStatus.Unknown;
    }

    internal void AddApplications(Applications applications, HealthCheckResult result)
    {
        var apps = new Dictionary<string, int>();

        if (applications != null)
        {
            IList<Application> registered = applications.GetRegisteredApplications();

            foreach (Application app in registered)
            {
                if (app.Instances?.Count > 0)
                {
                    apps.Add(app.Name, app.Instances.Count);
                }
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

    private long GetLastGoodRegistryFetchTimePeriod(long lastGoodRegistryFetchTimestamp)
    {
        return lastGoodRegistryFetchTimestamp <= 0L ? lastGoodRegistryFetchTimestamp : DateTime.UtcNow.Ticks - lastGoodRegistryFetchTimestamp;
    }

    private long GetLastGoodHeartbeatTimePeriod(long lastGoodHeartbeatTimestamp)
    {
        return lastGoodHeartbeatTimestamp <= 0L ? lastGoodHeartbeatTimestamp : DateTime.UtcNow.Ticks - lastGoodHeartbeatTimestamp;
    }
}
