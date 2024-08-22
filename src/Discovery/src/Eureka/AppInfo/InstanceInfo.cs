// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Text.Json;
using Steeltoe.Discovery.Eureka.Configuration;
using Steeltoe.Discovery.Eureka.Transport;
using Steeltoe.Discovery.Eureka.Util;

namespace Steeltoe.Discovery.Eureka.AppInfo;

/// <summary>
/// Represents an application instance in Eureka server.
/// </summary>
public sealed class InstanceInfo
{
    private static readonly ReadOnlyDictionary<string, string?> EmptyMetadata = new(new Dictionary<string, string?>());

    private volatile IReadOnlyDictionary<string, string?> _metadata = EmptyMetadata;
    private volatile NullableValueWrapper<InstanceStatus> _status = new(null);
    private volatile NullableValueWrapper<InstanceStatus> _overriddenStatus = new(null);
    private volatile NullableValueWrapper<DateTime> _lastUpdatedTimeUtc = new(null);
    private volatile NullableValueWrapper<DateTime> _lastDirtyTimeUtc = new(null);
    private volatile bool _isDirty;

    internal static InstanceInfo Disabled { get; } = new("disabled", "disabled", "disabled", "disabled", new DataCenterInfo
    {
        Name = DataCenterName.MyOwn
    });

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
    /// Gets the status of the instance. The status <see cref="InstanceStatus.Up" /> means the instance is ready to service requests.
    /// </summary>
    public InstanceStatus? Status
    {
        get => _status.Value;
        init => _status = new NullableValueWrapper<InstanceStatus>(value);
    }

    /// <summary>
    /// Gets the status being overridden by some other external process. This is mostly used in putting an instance out of service to block traffic to it.
    /// </summary>
    public InstanceStatus? OverriddenStatus
    {
        get => _overriddenStatus.Value;
        init => _overriddenStatus = new NullableValueWrapper<InstanceStatus>(value);
    }

    /// <summary>
    /// Gets the (possibly overridden) instance status.
    /// </summary>
    public InstanceStatus EffectiveStatus
    {
        get
        {
            if (OverriddenStatus != null && OverriddenStatus != InstanceStatus.Unknown)
            {
                return OverriddenStatus.Value;
            }

            return Status ?? InstanceStatus.Unknown;
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
    /// Gets the lease information for this instance.
    /// </summary>
    public LeaseInfo? LeaseInfo { get; init; }

    /// <summary>
    /// Gets a value indicating whether this instance is the coordinating discovery server.
    /// </summary>
    public bool? IsCoordinatingDiscoveryServer { get; init; }

    /// <summary>
    /// Gets application-specific metadata on the instance.
    /// </summary>
    public IReadOnlyDictionary<string, string?> Metadata
    {
        get => _metadata;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            _metadata = WithoutEmptyMetadataValues(value);
        }
    }

    /// <summary>
    /// Gets the time, in UTC, when the instance status was last updated.
    /// </summary>
    public DateTime? LastUpdatedTimeUtc
    {
        get => _lastUpdatedTimeUtc.Value;
        init => _lastUpdatedTimeUtc = new NullableValueWrapper<DateTime>(value);
    }

    /// <summary>
    /// Gets the time, in UTC, when this instance was last touched.
    /// </summary>
    // Suppressed because this needs to be public init-only, while private settable.
    public DateTime? LastDirtyTimeUtc
    {
        get => _lastDirtyTimeUtc.Value;
        init => _lastDirtyTimeUtc = new NullableValueWrapper<DateTime>(value);
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
                DateTime now = DateTime.UtcNow;

                _lastDirtyTimeUtc = new NullableValueWrapper<DateTime>(now);
                _lastUpdatedTimeUtc = new NullableValueWrapper<DateTime>(now);
                _isDirty = true;
            }
            else
            {
                _isDirty = false;
            }
        }
    }

    internal InstanceInfo(string instanceId, string appName, string hostName, string ipAddress, DataCenterInfo dataCenterInfo)
    {
        // These are required to register an instance to Eureka server, so they should always be available.
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(appName);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostName);
        ArgumentException.ThrowIfNullOrWhiteSpace(ipAddress);
        ArgumentNullException.ThrowIfNull(dataCenterInfo);

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

    /// <inheritdoc />
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, DebugSerializerOptions.Instance);
    }

    internal static InstanceInfo FromConfiguration(EurekaInstanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.IPAddress))
        {
            throw new InvalidOperationException(
                $"Configuration setting {EurekaInstanceOptions.ConfigurationPrefix}:{nameof(EurekaInstanceOptions.IPAddress)} must be provided.");
        }

        if (string.IsNullOrWhiteSpace(options.HostName))
        {
            throw new InvalidOperationException(
                $"Configuration setting {EurekaInstanceOptions.ConfigurationPrefix}:{nameof(EurekaInstanceOptions.HostName)} must be provided.");
        }

        if (string.IsNullOrWhiteSpace(options.InstanceId))
        {
            throw new InvalidOperationException(
                $"Configuration setting {EurekaInstanceOptions.ConfigurationPrefix}:{nameof(EurekaInstanceOptions.InstanceId)} must be provided.");
        }

        if (string.IsNullOrWhiteSpace(options.AppName))
        {
            throw new InvalidOperationException(
                $"Configuration setting {EurekaInstanceOptions.ConfigurationPrefix}:{nameof(EurekaInstanceOptions.AppName)} must be provided.");
        }

        int nonSecurePort = options.NonSecurePort is null or <= 0 ? 0 : options.NonSecurePort.Value;
        int securePort = options.SecurePort is null or <= 0 ? 0 : options.SecurePort.Value;

        int? nullableNonSecurePort = options.IsNonSecurePortEnabled ? nonSecurePort : null;
        int? nullableSecurePort = options.IsSecurePortEnabled ? securePort : null;

        return new InstanceInfo(options.InstanceId, options.AppName.ToUpperInvariant(), options.HostName, options.IPAddress, options.DataCenterInfo)
        {
            AppGroupName = options.AppGroupName?.ToUpperInvariant(),
            VipAddress = options.VipAddress,
            SecureVipAddress = options.SecureVipAddress,
            NonSecurePort = nonSecurePort,
            IsNonSecurePortEnabled = options.IsNonSecurePortEnabled,
            SecurePort = securePort,
            IsSecurePortEnabled = options.IsSecurePortEnabled,
            Status = options.IsInstanceEnabledOnInit ? InstanceStatus.Up : InstanceStatus.Starting,
            HomePageUrl = MakeUrl(options.HostName, nullableSecurePort, nullableNonSecurePort, options.HomePageUrlPath, options.HomePageUrl),
            StatusPageUrl = MakeUrl(options.HostName, nullableSecurePort, nullableNonSecurePort, options.StatusPageUrlPath, options.StatusPageUrl),
            HealthCheckUrl = MakeUrl(options.HostName, nullableSecurePort, nullableNonSecurePort, options.HealthCheckUrlPath, options.HealthCheckUrl),
            SecureHealthCheckUrl = MakeUrl(options.HostName, nullableSecurePort, null, options.HealthCheckUrlPath, options.SecureHealthCheckUrl),
            LeaseInfo = LeaseInfo.FromConfiguration(options),
            Metadata = options.MetadataMap.AsReadOnly(),
            AutoScalingGroupName = options.AutoScalingGroupName,
            IsDirty = true
        };
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
            AppGroupName = jsonInstance.AppGroupName,
            VipAddress = jsonInstance.VipAddress,
            SecureVipAddress = jsonInstance.SecureVipAddress,
            NonSecurePort = jsonInstance.Port?.Port ?? 0,
            IsNonSecurePortEnabled = jsonInstance.Port is { Enabled: true },
            SecurePort = jsonInstance.SecurePort?.Port ?? 0,
            IsSecurePortEnabled = jsonInstance.SecurePort is { Enabled: true },
            Status = jsonInstance.Status,
            OverriddenStatus = jsonInstance.OverriddenStatus ?? jsonInstance.OverriddenStatusLegacy,
            HomePageUrl = jsonInstance.HomePageUrl,
            StatusPageUrl = jsonInstance.StatusPageUrl,
            HealthCheckUrl = jsonInstance.HealthCheckUrl,
            SecureHealthCheckUrl = jsonInstance.SecureHealthCheckUrl,
            LeaseInfo = LeaseInfo.FromJson(jsonInstance.LeaseInfo),
            IsCoordinatingDiscoveryServer = jsonInstance.IsCoordinatingDiscoveryServer,
            Metadata = GetMetaDataFromJson(jsonInstance.Metadata),
            LastUpdatedTimeUtc = DateTimeConversions.FromNullableJavaMilliseconds(jsonInstance.LastUpdatedTimestamp),
            LastDirtyTimeUtc = DateTimeConversions.FromNullableJavaMilliseconds(jsonInstance.LastDirtyTimestamp),
            ActionType = jsonInstance.ActionType,
            AutoScalingGroupName = jsonInstance.AutoScalingGroupName,
            CountryId = jsonInstance.CountryId,
            Sid = jsonInstance.Sid,
            _isDirty = false
        };
    }

    internal JsonInstanceInfo ToJson()
    {
        return new JsonInstanceInfo
        {
            InstanceId = InstanceId,
            AppName = AppName,
            AppGroupName = AppGroupName,
            HostName = HostName,
            IPAddress = IPAddress,
            DataCenterInfo = DataCenterInfo.ToJson(),
            VipAddress = VipAddress,
            SecureVipAddress = SecureVipAddress,
            Port = new JsonPortWrapper
            {
                // Always send non-secure/secure ports, otherwise Eureka silently assumes 7000/7001.
                Enabled = IsNonSecurePortEnabled,
                Port = NonSecurePort
            },
            SecurePort = new JsonPortWrapper
            {
                Enabled = IsSecurePortEnabled,
                Port = SecurePort
            },
            Status = Status,
            // Workaround for https://github.com/Netflix/eureka/issues/1541.
            OverriddenStatusLegacy = OverriddenStatus ?? InstanceStatus.Unknown,
            HomePageUrl = HomePageUrl,
            StatusPageUrl = StatusPageUrl,
            HealthCheckUrl = HealthCheckUrl,
            SecureHealthCheckUrl = SecureHealthCheckUrl,
            LeaseInfo = LeaseInfo?.ToJson(),
            IsCoordinatingDiscoveryServer = IsCoordinatingDiscoveryServer,
            Metadata = Metadata.Count == 0
                ? new Dictionary<string, string?>
                {
                    { "@class", "java.util.Collections$EmptyMap" }
                }
                : Metadata.ToDictionary(pair => pair.Key, pair => pair.Value),
            LastUpdatedTimestamp = DateTimeConversions.ToNullableJavaMilliseconds(LastUpdatedTimeUtc),
            LastDirtyTimestamp = DateTimeConversions.ToNullableJavaMilliseconds(LastDirtyTimeUtc),
            ActionType = ActionType,
            AutoScalingGroupName = AutoScalingGroupName,
            CountryId = CountryId,
            Sid = Sid
        };
    }

    private static ReadOnlyDictionary<string, string?> GetMetaDataFromJson(IDictionary<string, string?>? jsonMetaData)
    {
        if (jsonMetaData == null || (jsonMetaData.TryGetValue("@class", out string? value) && value == "java.util.Collections$EmptyMap"))
        {
            return EmptyMetadata;
        }

        return jsonMetaData.AsReadOnly();
    }

    private static string? GetInstanceIdFromJson(JsonInstanceInfo jsonInstance)
    {
        if (string.IsNullOrWhiteSpace(jsonInstance.InstanceId))
        {
            if (jsonInstance.Metadata == null)
            {
                return null;
            }

            if (jsonInstance.Metadata.TryGetValue("instanceId", out string? metaDataInstanceId) && !string.IsNullOrEmpty(metaDataInstanceId))
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

    internal void DetectChanges(InstanceInfo previousInstance)
    {
        ArgumentNullException.ThrowIfNull(previousInstance);

        if (previousInstance.IsDirty)
        {
            // For some reason, the previous dirty instance was never sent, so we'll need to catch up.
            IsDirty = true;
        }
        else
        {
            // Intentionally skipping LastUpdatedTimeUtc and LastDirtyTimeUtc below, because they are not real changes.

#pragma warning disable S1067 // Expressions should not be too complex
            IsDirty = InstanceId != previousInstance.InstanceId || AppName != previousInstance.AppName || AppGroupName != previousInstance.AppGroupName ||
                HostName != previousInstance.HostName || IPAddress != previousInstance.IPAddress ||
                DataCenterInfo.Name != previousInstance.DataCenterInfo.Name || VipAddress != previousInstance.VipAddress ||
                SecureVipAddress != previousInstance.SecureVipAddress || NonSecurePort != previousInstance.NonSecurePort ||
                IsNonSecurePortEnabled != previousInstance.IsNonSecurePortEnabled || SecurePort != previousInstance.SecurePort ||
                IsSecurePortEnabled != previousInstance.IsSecurePortEnabled || Status != previousInstance.Status ||
                OverriddenStatus != previousInstance.OverriddenStatus || HomePageUrl != previousInstance.HomePageUrl ||
                StatusPageUrl != previousInstance.StatusPageUrl || HealthCheckUrl != previousInstance.HealthCheckUrl ||
                SecureHealthCheckUrl != previousInstance.SecureHealthCheckUrl || !Equals(LeaseInfo, previousInstance.LeaseInfo) ||
                IsCoordinatingDiscoveryServer != previousInstance.IsCoordinatingDiscoveryServer ||
                (!ReferenceEquals(Metadata, previousInstance.Metadata) && !Metadata.SequenceEqual(previousInstance.Metadata)) ||
                ActionType != previousInstance.ActionType || AutoScalingGroupName != previousInstance.AutoScalingGroupName ||
                CountryId != previousInstance.CountryId || Sid != previousInstance.Sid;
#pragma warning restore S1067 // Expressions should not be too complex
        }
    }

    // CAUTION: All Replace* methods must only be invoked from EurekaApplicationInfoManager, inside an exclusive lock.
    // This is needed to avoid sending a partially updated instance to Eureka, and to prevent concurrent updates overwriting in-flight changes.

    internal void ReplaceMetadata(IReadOnlyDictionary<string, string?> newMetadata)
    {
        ArgumentNullException.ThrowIfNull(newMetadata);

        newMetadata = WithoutEmptyMetadataValues(newMetadata);
        bool hasChanged = !ReferenceEquals(Metadata, newMetadata) && !Metadata.SequenceEqual(newMetadata);

        if (hasChanged)
        {
            _metadata = newMetadata;
            IsDirty = true;
        }
    }

    internal void ReplaceStatus(InstanceStatus? newStatus)
    {
        bool hasChanged = _status.Value != newStatus;

        if (hasChanged)
        {
            _status = new NullableValueWrapper<InstanceStatus>(newStatus);
            IsDirty = true;
        }
    }

    internal void ReplaceOverriddenStatus(InstanceStatus? newOverriddenStatus)
    {
        bool hasChanged = _overriddenStatus.Value != newOverriddenStatus;

        if (hasChanged)
        {
            _overriddenStatus = new NullableValueWrapper<InstanceStatus>(newOverriddenStatus);
            IsDirty = true;
        }
    }

    private static ReadOnlyDictionary<string, string?> WithoutEmptyMetadataValues(IReadOnlyDictionary<string, string?> source)
    {
        if (source.Count > 0)
        {
            Dictionary<string, string?> pruned = source.Where(pair => !string.IsNullOrEmpty(pair.Value)).ToDictionary(pair => pair.Key, pair => pair.Value);

            if (pruned.Count > 0)
            {
                return pruned.AsReadOnly();
            }
        }

        return EmptyMetadata;
    }
}
