// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using Steeltoe.Common;
using Steeltoe.Discovery.Eureka.Configuration;
using Steeltoe.Discovery.Eureka.Transport;
using Steeltoe.Discovery.Eureka.Util;

namespace Steeltoe.Discovery.Eureka.AppInfo;

public sealed class InstanceInfo
{
    private static readonly ReadOnlyDictionary<string, string?> EmptyMetadata = new(new Dictionary<string, string?>());

    private InstanceStatus? _status;
    private IReadOnlyDictionary<string, string?> _metaData = EmptyMetadata;
    private bool _isDirty;
    private InstanceStatus? _overriddenStatus;
    private LeaseInfo? _leaseInfo;

    private DateTime? _lastDirtyTimeUtc;

    /// <summary>
    /// Gets the ID that uniquely identifies this instance within a Eureka server.
    /// </summary>
    public string InstanceId { get; }

    /// <summary>
    /// Gets the application name of the instance. This is mostly used in querying of instances.
    /// </summary>
    public string AppName { get; }

    public string? AppGroupName { get; init; }

    /// <summary>
    /// Gets the fully qualified hostname of this running instance. This is mostly used in constructing the URL for communicating with the instance.
    /// </summary>
    public string HostName { get; }

    /// <summary>
    /// Gets the IP address of this running instance.
    /// </summary>
    public string IPAddress { get; }

    /// <summary>
    /// Gets the datacenter information for where this instance is running.
    /// </summary>
    public DataCenterInfo DataCenterInfo { get; }

    /// <summary>
    /// Gets the Virtual Internet Protocol address(es) for this instance. Multiple values can be specified as a comma-separated list. When using service
    /// discovery, virtual addresses are resolved into real addresses on outgoing HTTP requests.
    /// </summary>
    public string? VipAddress { get; init; }

    /// <summary>
    /// Gets the Secure Virtual Internet Protocol address(es) for this instance. Multiple values can be specified as a comma-separated list. When using
    /// service discovery, virtual addresses are resolved into real addresses on outgoing HTTP requests.
    /// </summary>
    public string? SecureVipAddress { get; init; }

    /// <summary>
    /// Gets the port number that is used to service requests.
    /// </summary>
    public int NonSecurePort { get; init; }

    /// <summary>
    /// Gets a value indicating whether <see cref="NonSecurePort" /> is enabled.
    /// </summary>
    public bool IsNonSecurePortEnabled { get; init; }

    /// <summary>
    /// Gets the secure port number that is used to service requests.
    /// </summary>
    public int SecurePort { get; init; }

    /// <summary>
    /// Gets a value indicating whether <see cref="SecurePort" /> is enabled.
    /// </summary>
    public bool IsSecurePortEnabled { get; init; }

    /// <summary>
    /// Gets or sets the status of the instance. The status <see cref="InstanceStatus.Up" /> means the instance is ready to service requests.
    /// </summary>
    public InstanceStatus? Status
    {
        get => _status;
        set
        {
            if (value != _status)
            {
                _status = value;
                IsDirty = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the status being overridden by some other external process. This is mostly used in putting an instance out of service to block traffic
    /// to it.
    /// </summary>
    public InstanceStatus? OverriddenStatus
    {
        get => _overriddenStatus;
        set
        {
            if (value != _overriddenStatus)
            {
                _overriddenStatus = value;
                IsDirty = true;
            }
        }
    }

    /// <summary>
    /// Gets the computed URL for the home page. Prefers secure when both ports are enabled.
    /// </summary>
    public string? HomePageUrl { get; init; }

    /// <summary>
    /// Gets the computed URL for the status page. Prefers secure when both ports are enabled.
    /// </summary>
    public string? StatusPageUrl { get; init; }

    /// <summary>
    /// Gets the computed URL for health checks. Prefers secure when both ports are enabled.
    /// </summary>
    public string? HealthCheckUrl { get; init; }

    /// <summary>
    /// Gets the computed URL for secure health checks. This is only available if the secure port is enabled.
    /// </summary>
    public string? SecureHealthCheckUrl { get; init; }

    /// <summary>
    /// Gets or sets the lease information for this instance.
    /// </summary>
    public LeaseInfo? LeaseInfo
    {
        get => _leaseInfo;
        set
        {
            if (!Equals(_leaseInfo, value))
            {
                _leaseInfo = value;
                IsDirty = true;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether this instance is the coordinating discovery server.
    /// </summary>
    public bool? IsCoordinatingDiscoveryServer { get; init; }

    /// <summary>
    /// Gets or sets application-specific metadata on the instance.
    /// </summary>
    public IReadOnlyDictionary<string, string?> Metadata
    {
        get => _metaData;
        set
        {
            ArgumentGuard.NotNull(value);

            if (!ReferenceEquals(value, _metaData) && !value.SequenceEqual(_metaData))
            {
                _metaData = value;
                IsDirty = true;
            }
        }
    }

    /// <summary>
    /// Gets the time, in UTC, when the instance status was last updated.
    /// </summary>
    public DateTime? LastUpdatedTimeUtc { get; init; }

    /// <summary>
    /// Gets the time, in UTC, when this instance was last touched.
    /// </summary>
#pragma warning disable S2292 // Trivial properties should be auto-implemented
    // Suppressed because this needs to be public init-only, while private settable.
    public DateTime? LastDirtyTimeUtc
#pragma warning restore S2292 // Trivial properties should be auto-implemented
    {
        get => _lastDirtyTimeUtc;
        init => _lastDirtyTimeUtc = value;
    }

    /// <summary>
    /// Gets the type of action done on the instance in the server. Primarily used for updating deltas in the <see cref="EurekaDiscoveryClient" /> instance.
    /// </summary>
    public ActionType? ActionType { get; init; }

    /// <summary>
    /// Gets the AWS autoscaling group name for this instance.
    /// </summary>
    public string? AutoScalingGroupName { get; init; }

    public int? CountryId { get; init; }

    /// <summary>
    /// Gets the identity of this application instance (deprecated).
    /// </summary>
    public string? Sid { get; init; }

    /// <summary>
    /// Gets a value indicating whether any state changed, so that <see cref="EurekaDiscoveryClient" /> re-registers on the next heartbeat.
    /// </summary>
    public bool IsDirty
    {
        get => _isDirty;
        internal set
        {
            if (value)
            {
                _lastDirtyTimeUtc = DateTime.UtcNow;
            }

            _isDirty = value;
        }
    }

    public InstanceInfo(string instanceId, string appName, string hostName, string ipAddress, DataCenterInfo dataCenterInfo)
    {
        // These are required to register an instance to Eureka server, so they should always be available.
        ArgumentGuard.NotNullOrWhiteSpace(instanceId);
        ArgumentGuard.NotNullOrWhiteSpace(appName);
        ArgumentGuard.NotNullOrWhiteSpace(hostName);
        ArgumentGuard.NotNullOrWhiteSpace(ipAddress);
        ArgumentGuard.NotNull(dataCenterInfo);

        InstanceId = instanceId;
        AppName = appName;
        HostName = hostName;
        IPAddress = ipAddress;
        DataCenterInfo = dataCenterInfo;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not InstanceInfo other)
        {
            return false;
        }

        return InstanceId == other.InstanceId;
    }

    public override int GetHashCode()
    {
        return InstanceId.GetHashCode();
    }

    public override string ToString()
    {
        return $"Instance[{nameof(InstanceId)}={InstanceId},{nameof(HostName)}={HostName},{nameof(IPAddress)}={IPAddress},{nameof(Status)}={Status}," +
            $"{nameof(NonSecurePort)}={NonSecurePort},{nameof(IsNonSecurePortEnabled)}={IsNonSecurePortEnabled},{nameof(SecurePort)}={SecurePort}," +
            $"{nameof(IsSecurePortEnabled)}={IsSecurePortEnabled},{nameof(VipAddress)}={VipAddress},{nameof(SecureVipAddress)}={SecureVipAddress},{nameof(ActionType)}={ActionType}]";
    }

    internal static InstanceInfo FromConfiguration(EurekaInstanceOptions options)
    {
        ArgumentGuard.NotNull(options);

        if (string.IsNullOrWhiteSpace(options.IPAddress))
        {
            throw new InvalidOperationException(
                $"Configuration setting {EurekaInstanceOptions.ConfigurationPrefix}:{nameof(EurekaInstanceOptions.IPAddress)} must be provided.");
        }

        string? hostName = options.ResolveHostName(false);
        string? instanceId = string.IsNullOrWhiteSpace(options.InstanceId) ? hostName : options.InstanceId;

        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new InvalidOperationException(
                $"Configuration setting {EurekaInstanceOptions.ConfigurationPrefix}:{nameof(EurekaInstanceOptions.InstanceId)} must be provided.");
        }

        if (options.PreferIPAddress || string.IsNullOrWhiteSpace(hostName))
        {
            hostName = options.IPAddress;
        }

        string appName = (options.AppName ?? EurekaInstanceOptions.DefaultAppName).ToUpperInvariant();

        int nonSecurePort = options.NonSecurePort <= 0 ? EurekaInstanceOptions.DefaultNonSecurePort : options.NonSecurePort;
        int securePort = options.SecurePort <= 0 ? EurekaInstanceOptions.DefaultSecurePort : options.SecurePort;

        int? nullableNonSecurePort = options.IsNonSecurePortEnabled ? nonSecurePort : null;
        int? nullableSecurePort = options.IsSecurePortEnabled ? securePort : null;

        DateTime utcNow = DateTime.UtcNow;

        return new InstanceInfo(instanceId, appName, hostName, options.IPAddress, options.DataCenterInfo)
        {
            AppGroupName = options.AppGroupName?.ToUpperInvariant(),
            NonSecurePort = nonSecurePort,
            IsNonSecurePortEnabled = options.IsNonSecurePortEnabled,
            SecurePort = securePort,
            IsSecurePortEnabled = options.IsSecurePortEnabled,
            VipAddress = options.VirtualHostName,
            SecureVipAddress = options.SecureVirtualHostName,
            AutoScalingGroupName = options.AutoScalingGroupName,
            _status = options.IsInstanceEnabledOnInit ? InstanceStatus.Up : InstanceStatus.Starting,
            LastUpdatedTimeUtc = utcNow,
            _lastDirtyTimeUtc = utcNow,
            _leaseInfo = LeaseInfo.FromConfiguration(options),
            _metaData = options.MetadataMap.Count > 0 ? new ReadOnlyDictionary<string, string?>(options.MetadataMap) : EmptyMetadata,
            HomePageUrl = MakeUrl(hostName, nullableSecurePort, nullableNonSecurePort, options.HomePageUrlPath, options.HomePageUrl),
            StatusPageUrl = MakeUrl(hostName, nullableSecurePort, nullableNonSecurePort, options.StatusPageUrlPath, options.StatusPageUrl),
            HealthCheckUrl = MakeUrl(hostName, nullableSecurePort, nullableNonSecurePort, options.HealthCheckUrlPath, options.HealthCheckUrl),
            SecureHealthCheckUrl = MakeUrl(hostName, nullableSecurePort, null, options.HealthCheckUrlPath, options.SecureHealthCheckUrl),
            _isDirty = false
        };
    }

    internal void UpdateFromConfiguration(EurekaInstanceOptions options)
    {
        ArgumentGuard.NotNull(options);

        // Marks this instance as dirty when settings have changed.
        LeaseInfo = LeaseInfo.FromConfiguration(options);
        Metadata = options.MetadataMap.Count > 0 ? new ReadOnlyDictionary<string, string?>(options.MetadataMap) : EmptyMetadata;
    }

    internal static InstanceInfo? FromJson(JsonInstanceInfo? jsonInstance)
    {
        if (jsonInstance == null)
        {
            return null;
        }

        string? instanceId = GetInstanceIdFromJson(jsonInstance);

        if (instanceId == null || string.IsNullOrWhiteSpace(jsonInstance.AppName) || string.IsNullOrWhiteSpace(jsonInstance.HostName) ||
            string.IsNullOrWhiteSpace(jsonInstance.IPAddress))
        {
            return null;
        }

        DataCenterInfo? dataCenterInfo = DataCenterInfo.FromJson(jsonInstance.DataCenterInfo);

        if (dataCenterInfo == null)
        {
            return null;
        }

        return new InstanceInfo(instanceId, jsonInstance.AppName, jsonInstance.HostName, jsonInstance.IPAddress, dataCenterInfo)
        {
            Sid = jsonInstance.Sid,
            AppGroupName = jsonInstance.AppGroupName,
            NonSecurePort = jsonInstance.Port?.Port ?? 0,
            IsNonSecurePortEnabled = jsonInstance.Port is { Enabled: true },
            SecurePort = jsonInstance.SecurePort?.Port ?? 0,
            IsSecurePortEnabled = jsonInstance.SecurePort is { Enabled: true },
            HomePageUrl = jsonInstance.HomePageUrl,
            StatusPageUrl = jsonInstance.StatusPageUrl,
            HealthCheckUrl = jsonInstance.HealthCheckUrl,
            SecureHealthCheckUrl = jsonInstance.SecureHealthCheckUrl,
            VipAddress = jsonInstance.VipAddress,
            SecureVipAddress = jsonInstance.SecureVipAddress,
            CountryId = jsonInstance.CountryId,
            _status = jsonInstance.Status,
            OverriddenStatus = jsonInstance.OverriddenStatus ?? jsonInstance.OverriddenStatusLegacy,
            _leaseInfo = LeaseInfo.FromJson(jsonInstance.LeaseInfo),
            IsCoordinatingDiscoveryServer = jsonInstance.IsCoordinatingDiscoveryServer,
            LastUpdatedTimeUtc = DateTimeConversions.FromNullableJavaMilliseconds(jsonInstance.LastUpdatedTimestamp),
            _lastDirtyTimeUtc = DateTimeConversions.FromNullableJavaMilliseconds(jsonInstance.LastDirtyTimestamp),
            ActionType = jsonInstance.ActionType,
            AutoScalingGroupName = jsonInstance.AutoScalingGroupName,
            _metaData = GetMetaDataFromJson(jsonInstance.Metadata),
            _isDirty = false
        };
    }

    internal JsonInstanceInfo ToJson()
    {
        return new JsonInstanceInfo
        {
            InstanceId = InstanceId,
            Sid = Sid,
            AppName = AppName,
            AppGroupName = AppGroupName,
            IPAddress = IPAddress,
            Port = new JsonPortWrapper
            {
                Enabled = IsNonSecurePortEnabled,
                Port = NonSecurePort
            },
            SecurePort = new JsonPortWrapper
            {
                Enabled = IsSecurePortEnabled,
                Port = SecurePort
            },
            HomePageUrl = HomePageUrl,
            StatusPageUrl = StatusPageUrl,
            HealthCheckUrl = HealthCheckUrl,
            SecureHealthCheckUrl = SecureHealthCheckUrl,
            VipAddress = VipAddress,
            SecureVipAddress = SecureVipAddress,
            CountryId = CountryId,
            DataCenterInfo = DataCenterInfo.ToJson(),
            HostName = HostName,
            Status = Status,
            OverriddenStatus = OverriddenStatus,
            LeaseInfo = LeaseInfo?.ToJson(),
            IsCoordinatingDiscoveryServer = IsCoordinatingDiscoveryServer,
            LastUpdatedTimestamp = DateTimeConversions.ToNullableJavaMilliseconds(LastUpdatedTimeUtc),
            LastDirtyTimestamp = DateTimeConversions.ToNullableJavaMilliseconds(LastDirtyTimeUtc),
            ActionType = ActionType,
            AutoScalingGroupName = AutoScalingGroupName,
            Metadata = Metadata.Count == 0
                ? new Dictionary<string, string?>
                {
                    { "@class", "java.util.Collections$EmptyMap" }
                }
                : Metadata.ToDictionary(pair => pair.Key, pair => pair.Value)
        };
    }

    private static IReadOnlyDictionary<string, string?> GetMetaDataFromJson(IDictionary<string, string?>? jsonMetaData)
    {
        if (jsonMetaData == null || (jsonMetaData.TryGetValue("@class", out string? value) && value == "java.util.Collections$EmptyMap"))
        {
            return EmptyMetadata;
        }

        return new ReadOnlyDictionary<string, string?>(jsonMetaData);
    }

    private static string? GetInstanceIdFromJson(JsonInstanceInfo jsonInstance)
    {
        if (string.IsNullOrWhiteSpace(jsonInstance.InstanceId))
        {
            if (jsonInstance.Metadata == null)
            {
                return null;
            }

            if (jsonInstance.Metadata.TryGetValue("instanceId", out string? metaDataInstanceId) && metaDataInstanceId != null)
            {
                return $"{jsonInstance.HostName}:{metaDataInstanceId}";
            }

            return null;
        }

        return jsonInstance.InstanceId;
    }

    private static string? MakeUrl(string hostName, int? securePort, int? nonSecurePort, string? relativeUrl, string? explicitUrl)
    {
        if (!string.IsNullOrWhiteSpace(explicitUrl))
        {
            return explicitUrl.Replace("${eureka.hostname}", hostName, StringComparison.Ordinal);
        }

        if (!string.IsNullOrWhiteSpace(relativeUrl))
        {
            if (securePort != null)
            {
                return $"https://{hostName}:{securePort}{relativeUrl}";
            }

            if (nonSecurePort != null)
            {
                return $"http://{hostName}:{nonSecurePort}{relativeUrl}";
            }
        }

        return null;
    }
}
