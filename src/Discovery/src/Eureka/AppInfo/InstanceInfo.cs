// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Configuration;
using Steeltoe.Discovery.Eureka.Transport;
using Steeltoe.Discovery.Eureka.Util;

// TODO: NRT

namespace Steeltoe.Discovery.Eureka.AppInfo;

public sealed class InstanceInfo
{
    private string _sid;
    private InstanceStatus _status;
    private Dictionary<string, string> _metaData;
    private bool _isDirty;

    public string InstanceId { get; set; }

    /// <summary>
    /// Gets the application name of the instance. This is mostly used in querying of instances.
    /// </summary>
    public string AppName { get; internal set; }

    public string AppGroupName { get; private set; }

    /// <summary>
    /// Gets the IP address of this running instance.
    /// </summary>
    public string IPAddress { get; internal set; }

    /// <summary>
    /// Gets the identity of this application instance.
    /// </summary>
    public string Sid
    {
        get => _sid;
        internal set
        {
            _sid = value;
            IsDirty = true;
        }
    }

    /// <summary>
    /// Gets the port number that is used to service requests.
    /// </summary>
    public int Port { get; private set; }

    /// <summary>
    /// Gets the secure port that is used to service requests.
    /// </summary>
    public int SecurePort { get; private set; }

    /// <summary>
    /// Gets the value described at <see cref="EurekaInstanceOptions.HomePageUrl" />.
    /// </summary>
    public string HomePageUrl { get; private set; }

    /// <summary>
    /// Gets the value described at <see cref="EurekaInstanceOptions.StatusPageUrl" />.
    /// </summary>
    public string StatusPageUrl { get; private set; }

    /// <summary>
    /// Gets the value described at <see cref="EurekaInstanceOptions.HealthCheckUrl" />.
    /// </summary>
    public string HealthCheckUrl { get; private set; }

    /// <summary>
    /// Gets the value described at <see cref="EurekaInstanceOptions.SecureHealthCheckUrl" />.
    /// </summary>
    public string SecureHealthCheckUrl { get; private set; }

    /// <summary>
    /// Gets the Virtual Internet Protocol address for this instance. The address should follow the format
    /// <b>
    /// hostname:port
    /// </b>
    /// . This address needs to be resolved into a real address for communicating with this instance.
    /// </summary>
    public string VipAddress { get; internal set; }

    /// <summary>
    /// Gets the Secure Virtual Internet Protocol address for this instance. The address should follow the format
    /// <b>
    /// hostname:port
    /// </b>
    /// . This address needs to be resolved into a real address for communicating with this instance.
    /// </summary>
    public string SecureVipAddress { get; internal set; }

    public int CountryId { get; private set; }

    /// <summary>
    /// Gets the datacenter information for where this instance is running.
    /// </summary>
    public DataCenterInfo DataCenterInfo { get; private set; }

    /// <summary>
    /// Gets the fully qualified hostname of this running instance. This is mostly used in constructing the URL for communicating with the instance.
    /// </summary>
    public string HostName { get; internal set; }

    /// <summary>
    /// Gets the status of the instance. If the status is <see cref="InstanceStatus.Up" />, that is when the instance is ready to service requests.
    /// </summary>
    public InstanceStatus Status
    {
        get => _status;
        internal set
        {
            if (value != _status)
            {
                _status = value;
                IsDirty = true;
            }
        }
    }

    /// <summary>
    /// Gets the status overridden by some other external process. This is mostly used in putting an instance out of service to block traffic to it.
    /// </summary>
    public InstanceStatus OverriddenStatus { get; private set; }

    /// <summary>
    /// Gets the lease information for this instance.
    /// </summary>
    public LeaseInfo LeaseInfo { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether this instance is the coordinating discovery server.
    /// </summary>
    public bool IsCoordinatingDiscoveryServer { get; private set; }

    /// <summary>
    /// Gets all application-specific metadata set on the instance.
    /// </summary>
    public Dictionary<string, string> Metadata
    {
        get => _metaData;
        internal set
        {
            _metaData = value;
            IsDirty = true;
        }
    }

    /// <summary>
    /// Gets the time elapsed since epoch since the instance status has been last updated.
    /// </summary>
    public long LastUpdatedTimestamp { get; internal set; }

    /// <summary>
    /// Gets the last timestamp when this instance was touched.
    /// </summary>
    public long LastDirtyTimestamp { get; internal set; }

    /// <summary>
    /// Gets the type of action done on the instance in the server. Primarily used for updating deltas in the <see cref="EurekaDiscoveryClient" /> instance.
    /// </summary>
    public ActionType ActionType { get; internal set; }

    /// <summary>
    /// Gets the AWS autoscaling group name for this instance.
    /// </summary>
    public string AsgName { get; private set; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Port" /> is enabled.
    /// </summary>
    public bool IsInsecurePortEnabled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether <see cref="SecurePort" /> is enabled.
    /// </summary>
    public bool IsSecurePortEnabled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether any state changed, so that <see cref="EurekaDiscoveryClient" /> can check whether to retransmit info or not on the
    /// next heartbeat.
    /// </summary>
    public bool IsDirty
    {
        get => _isDirty;
        internal set
        {
            if (value)
            {
                LastDirtyTimestamp = DateTime.UtcNow.Ticks;
            }

            _isDirty = value;
        }
    }

    internal InstanceInfo()
    {
        OverriddenStatus = InstanceStatus.Unknown;
        IsSecurePortEnabled = false;
        IsCoordinatingDiscoveryServer = false;
        IsInsecurePortEnabled = true;
        CountryId = 1;
        Port = 7001;
        SecurePort = 7002;
        LastUpdatedTimestamp = LastDirtyTimestamp = DateTime.UtcNow.Ticks;
        _sid = "na";
        _metaData = [];
        _isDirty = false;
        _status = InstanceStatus.Up;
    }

    public override bool Equals(object obj)
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
        return InstanceId?.GetHashCode() ?? 0;
    }

    public override string ToString()
    {
        return
            $"Instance[InstanceId={InstanceId},HostName={HostName},IPAddress={IPAddress},Status={Status},Port={Port},IsInsecurePortEnabled={IsInsecurePortEnabled}," +
            $"SecurePort={SecurePort}IsSecurePortEnabled={IsSecurePortEnabled},VipAddress={VipAddress},SecureVipAddress={SecureVipAddress},ActionType={ActionType}]";
    }

    internal static InstanceInfo FromConfiguration(EurekaInstanceOptions options)
    {
        var info = new InstanceInfo
        {
            LeaseInfo = LeaseInfo.FromConfiguration(options),
            InstanceId = options.InstanceId
        };

        if (string.IsNullOrEmpty(info.InstanceId))
        {
            info.InstanceId = options.ResolveHostName(false);
        }

        string defaultAddress = options.ResolveHostName(false);

        if (options.PreferIPAddress || string.IsNullOrEmpty(defaultAddress))
        {
            defaultAddress = options.IPAddress;
        }

        info.AppName = (options.AppName ?? EurekaInstanceOptions.DefaultAppName).ToUpperInvariant();
        info.AppGroupName = options.AppGroupName?.ToUpperInvariant();
        info.DataCenterInfo = options.DataCenterInfo;
        info.IPAddress = options.IPAddress;
        info.HostName = defaultAddress;
        info.Port = options.NonSecurePort == -1 ? EurekaInstanceOptions.DefaultNonSecurePort : options.NonSecurePort;
        info.IsInsecurePortEnabled = options.IsNonSecurePortEnabled;
        info.SecurePort = options.SecurePort == -1 ? EurekaInstanceOptions.DefaultSecurePort : options.SecurePort;
        info.IsSecurePortEnabled = options.IsSecurePortEnabled;
        info.VipAddress = options.VirtualHostName;
        info.SecureVipAddress = options.SecureVirtualHostName;
        info.HomePageUrl = MakeUrl(info, options.HomePageUrlPath, options.HomePageUrl);
        info.StatusPageUrl = MakeUrl(info, options.StatusPageUrlPath, options.StatusPageUrl);
        info.AsgName = options.AsgName;
        info.HealthCheckUrl = MakeUrl(info, options.HealthCheckUrlPath, options.HealthCheckUrl, options.SecureHealthCheckUrl);

        if (!options.IsInstanceEnabledOnInit)
        {
            info._status = InstanceStatus.Starting;
        }

        if (options.MetadataMap.Count > 0)
        {
            info._metaData = new Dictionary<string, string>(options.MetadataMap);
        }

        return info;
    }

    internal static InstanceInfo FromJsonInstance(JsonInstanceInfo json)
    {
        var info = new InstanceInfo();

        if (json != null)
        {
            info._sid = json.Sid ?? "na";
            info.AppName = json.AppName;
            info.AppGroupName = json.AppGroupName;
            info.IPAddress = json.IPAddress;
            info.Port = json.Port?.Port ?? 0;
            info.IsInsecurePortEnabled = json.Port is { Enabled: true };
            info.SecurePort = json.SecurePort?.Port ?? 0;
            info.IsSecurePortEnabled = json.SecurePort is { Enabled: true };
            info.HomePageUrl = json.HomePageUrl;
            info.StatusPageUrl = json.StatusPageUrl;
            info.HealthCheckUrl = json.HealthCheckUrl;
            info.SecureHealthCheckUrl = json.SecureHealthCheckUrl;
            info.VipAddress = json.VipAddress;
            info.SecureVipAddress = json.SecureVipAddress;
            info.CountryId = json.CountryId;
            info.DataCenterInfo = DataCenterInfo.FromJson(json.DataCenterInfo);
            info.HostName = json.HostName;
            info.Status = json.Status;
            info.OverriddenStatus = json.OverriddenStatus;
            info.LeaseInfo = LeaseInfo.FromJson(json.LeaseInfo);
            info.IsCoordinatingDiscoveryServer = json.IsCoordinatingDiscoveryServer;
            info.LastUpdatedTimestamp = DateTimeConversions.FromJavaMilliseconds(json.LastUpdatedTimestamp).Ticks;
            info.LastDirtyTimestamp = DateTimeConversions.FromJavaMilliseconds(json.LastDirtyTimestamp).Ticks;
            info.ActionType = json.ActionType;
            info.AsgName = json.AsgName;
            info._metaData = GetMetaDataFromJson(json.Metadata);
            info.InstanceId = GetInstanceIdFromJson(json, info._metaData);
        }

        return info;
    }

    internal JsonInstanceInfo ToJsonInstance()
    {
        return new JsonInstanceInfo
        {
            InstanceId = InstanceId,
            Sid = Sid ?? "na",
            AppName = AppName,
            AppGroupName = AppGroupName,
            IPAddress = IPAddress,
            Port = JsonPortWrapper.Create(IsInsecurePortEnabled, Port),
            SecurePort = JsonPortWrapper.Create(IsSecurePortEnabled, SecurePort),
            HomePageUrl = HomePageUrl,
            StatusPageUrl = StatusPageUrl,
            HealthCheckUrl = HealthCheckUrl,
            SecureHealthCheckUrl = SecureHealthCheckUrl,
            VipAddress = VipAddress,
            SecureVipAddress = SecureVipAddress,
            CountryId = CountryId,
            DataCenterInfo = DataCenterInfo?.ToJson(),
            HostName = HostName,
            Status = Status,
            OverriddenStatus = OverriddenStatus,
            LeaseInfo = LeaseInfo?.ToJson(),
            IsCoordinatingDiscoveryServer = IsCoordinatingDiscoveryServer,
            LastUpdatedTimestamp = DateTimeConversions.ToJavaMilliseconds(new DateTime(LastUpdatedTimestamp, DateTimeKind.Utc)),
            LastDirtyTimestamp = DateTimeConversions.ToJavaMilliseconds(new DateTime(LastDirtyTimestamp, DateTimeKind.Utc)),
            ActionType = ActionType,
            AsgName = AsgName,
            Metadata = Metadata.Count == 0
                ? new Dictionary<string, string>
                {
                    { "@class", "java.util.Collections$EmptyMap" }
                }
                : Metadata
        };
    }

    private static Dictionary<string, string> GetMetaDataFromJson(IDictionary<string, string> json)
    {
        if (json == null)
        {
            return [];
        }

        if (json.TryGetValue("@class", out string value) && value == "java.util.Collections$EmptyMap")
        {
            return [];
        }

        return new Dictionary<string, string>(json);
    }

    private static string GetInstanceIdFromJson(JsonInstanceInfo instanceInfo, Dictionary<string, string> metaData)
    {
        if (string.IsNullOrEmpty(instanceInfo.InstanceId))
        {
            if (metaData == null)
            {
                return null;
            }

            if (metaData.TryGetValue("instanceId", out string mid))
            {
                return $"{instanceInfo.HostName}:{mid}";
            }

            return null;
        }

        return instanceInfo.InstanceId;
    }

    private static string MakeUrl(InstanceInfo instance, string relativeUrl, string explicitUrl, string secureExplicitUrl = null)
    {
        if (!string.IsNullOrEmpty(explicitUrl))
        {
            return explicitUrl;
        }

        if (!string.IsNullOrEmpty(relativeUrl) && instance.IsInsecurePortEnabled)
        {
            return $"http://{instance.HostName}:{instance.Port}{relativeUrl}";
        }

        if (!string.IsNullOrEmpty(secureExplicitUrl))
        {
            return secureExplicitUrl;
        }

        if (instance.IsSecurePortEnabled)
        {
            return $"https://{instance.HostName}:{instance.SecurePort}{relativeUrl}";
        }

        return string.Empty;
    }
}
