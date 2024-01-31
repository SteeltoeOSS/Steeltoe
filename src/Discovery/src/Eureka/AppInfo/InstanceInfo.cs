// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Steeltoe.Discovery.Eureka.Transport;
using Steeltoe.Discovery.Eureka.Util;

namespace Steeltoe.Discovery.Eureka.AppInfo;

public sealed class InstanceInfo
{
    private string _sid;

    private InstanceStatus _status;

    private Dictionary<string, string> _metaData;

    private bool _isDirty;

    public string InstanceId { get; internal set; }
    public string AppName { get; internal set; }
    public string AppGroupName { get; private set; }

    public string IPAddress { get; internal set; }

    public string Sid
    {
        get => _sid;
        internal set
        {
            _sid = value;
            IsDirty = true;
        }
    }

    public int Port { get; private set; }
    public int SecurePort { get; private set; }
    public string HomePageUrl { get; private set; }
    public string StatusPageUrl { get; private set; }
    public string HealthCheckUrl { get; private set; }
    public string SecureHealthCheckUrl { get; private set; }
    public string VipAddress { get; internal set; }
    public string SecureVipAddress { get; internal set; }
    public int CountryId { get; private set; }
    public DataCenterInfo DataCenterInfo { get; private set; }
    public string HostName { get; internal set; }

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

    public InstanceStatus OverriddenStatus { get; private set; }
    public LeaseInfo LeaseInfo { get; internal set; }
    public bool IsCoordinatingDiscoveryServer { get; private set; }

    public Dictionary<string, string> Metadata
    {
        get => _metaData;
        internal set
        {
            _metaData = value;
            IsDirty = true;
        }
    }

    public long LastUpdatedTimestamp { get; private set; }
    public long LastDirtyTimestamp { get; internal set; }
    public ActionType ActionType { get; internal set; }
    public string AsgName { get; private set; }
    public bool IsInsecurePortEnabled { get; private set; }
    public bool IsSecurePortEnabled { get; private set; }

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
        var sb = new StringBuilder("Instance[");
        sb.Append($"InstanceId={InstanceId}");
        sb.Append(',');
        sb.Append($"HostName={HostName}");
        sb.Append(',');
        sb.Append($"IpAddr={IPAddress}");
        sb.Append(',');
        sb.Append($"Status={Status}");
        sb.Append(',');
        sb.Append($"IsInsecurePortEnabled={IsInsecurePortEnabled}");
        sb.Append(',');
        sb.Append($"Port={Port}");
        sb.Append(',');
        sb.Append($"IsSecurePortEnabled={IsSecurePortEnabled}");
        sb.Append(',');
        sb.Append($"SecurePort={SecurePort}");
        sb.Append(',');
        sb.Append($"VipAddress={VipAddress}");
        sb.Append(',');
        sb.Append($"SecureVipAddress={SecureVipAddress}");
        sb.Append(',');
        sb.Append($"ActionType={ActionType}");
        sb.Append(']');
        return sb.ToString();
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

        info.AppName = options.AppName.ToUpperInvariant();
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

        if (!string.IsNullOrEmpty(info.InstanceId))
        {
            InstanceInfo me = EurekaApplicationInfoManager.SharedInstance.InstanceInfo;

            if (me != null && info.InstanceId == me.InstanceId)
            {
                info.IsCoordinatingDiscoveryServer = true;
            }
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
            info.DataCenterInfo = json.DataCenterInfo == null ? null : DataCenterInfo.FromJson(json.DataCenterInfo);
            info.HostName = json.HostName;
            info.Status = json.Status;
            info.OverriddenStatus = json.OverriddenStatus;
            info.LeaseInfo = LeaseInfo.FromJson(json.LeaseInfo);
            info.IsCoordinatingDiscoveryServer = json.IsCoordinatingDiscoveryServer;
            info.LastUpdatedTimestamp = DateTimeConversions.FromJavaMillis(json.LastUpdatedTimestamp).Ticks;
            info.LastDirtyTimestamp = DateTimeConversions.FromJavaMillis(json.LastDirtyTimestamp).Ticks;
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
            LastUpdatedTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(LastUpdatedTimestamp, DateTimeKind.Utc)),
            LastDirtyTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(LastDirtyTimestamp, DateTimeKind.Utc)),
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

    private static string MakeUrl(InstanceInfo info, string relativeUrl, string explicitUrl, string secureExplicitUrl = null)
    {
        if (!string.IsNullOrEmpty(explicitUrl))
        {
            return explicitUrl;
        }

        if (!string.IsNullOrEmpty(relativeUrl) && info.IsInsecurePortEnabled)
        {
            return $"http://{info.HostName}:{info.Port}{relativeUrl}";
        }

        if (!string.IsNullOrEmpty(secureExplicitUrl))
        {
            return secureExplicitUrl;
        }

        if (info.IsSecurePortEnabled)
        {
            return $"https://{info.HostName}:{info.SecurePort}{relativeUrl}";
        }

        return string.Empty;
    }
}
