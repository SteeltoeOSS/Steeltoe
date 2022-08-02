// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Text.Json.Serialization;
using Steeltoe.Discovery.Eureka.Transport;
using Steeltoe.Discovery.Eureka.Util;

namespace Steeltoe.Discovery.Eureka.AppInfo;

public class InstanceInfo
{
    private string _sid;

    private InstanceStatus _status;

    private Dictionary<string, string> _metaData;

    private bool _isDirty;

    public string InstanceId { get; internal set; }

    public string AppName { get; internal set; }

    public string AppGroupName { get; internal set; }

    [JsonPropertyName("IpAddr")]
    public string IpAddress { get; internal set; }

    public string Sid
    {
        get => _sid;

        internal set
        {
            _sid = value;
            IsDirty = true;
        }
    }

    public int Port { get; internal set; }

    public int SecurePort { get; internal set; }

    public string HomePageUrl { get; internal set; }

    public string StatusPageUrl { get; internal set; }

    public string HealthCheckUrl { get; internal set; }

    public string SecureHealthCheckUrl { get; internal set; }

    public string VipAddress { get; internal set; }

    public string SecureVipAddress { get; internal set; }

    public int CountryId { get; internal set; }

    public IDataCenterInfo DataCenterInfo { get; internal set; }

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

    public InstanceStatus OverriddenStatus { get; internal set; }

    public LeaseInfo LeaseInfo { get; internal set; }

    public bool IsCoordinatingDiscoveryServer { get; internal set; }

    public Dictionary<string, string> Metadata
    {
        get => _metaData;

        internal set
        {
            _metaData = value;
            IsDirty = true;
        }
    }

    public long LastUpdatedTimestamp { get; internal set; }

    public long LastDirtyTimestamp { get; internal set; }

    public ActionType ActionType { get; internal set; }

    public string AsgName { get; internal set; }

    public bool IsInsecurePortEnabled { get; internal set; }

    public bool IsSecurePortEnabled { get; internal set; }

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
        _metaData = new Dictionary<string, string>();
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
        sb.Append($"IpAddr={IpAddress}");
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

    internal static InstanceInfo FromInstanceConfig(IEurekaInstanceConfig instanceConfig)
    {
        var info = new InstanceInfo
        {
            LeaseInfo = LeaseInfo.FromConfig(instanceConfig),
            InstanceId = instanceConfig.InstanceId
        };

        if (string.IsNullOrEmpty(info.InstanceId))
        {
            info.InstanceId = instanceConfig.GetHostName(false);
        }

        string defaultAddress = instanceConfig.GetHostName(false);

        if (instanceConfig.PreferIpAddress || string.IsNullOrEmpty(defaultAddress))
        {
            defaultAddress = instanceConfig.IpAddress;
        }

        info.AppName = instanceConfig.AppName.ToUpperInvariant();
        info.AppGroupName = instanceConfig.AppGroupName?.ToUpperInvariant();
        info.DataCenterInfo = instanceConfig.DataCenterInfo;
        info.IpAddress = instanceConfig.IpAddress;
        info.HostName = defaultAddress;
        info.Port = instanceConfig.NonSecurePort == -1 ? EurekaInstanceConfig.DefaultNonSecurePort : instanceConfig.NonSecurePort;
        info.IsInsecurePortEnabled = instanceConfig.IsNonSecurePortEnabled;
        info.SecurePort = instanceConfig.SecurePort == -1 ? EurekaInstanceConfig.DefaultSecurePort : instanceConfig.SecurePort;
        info.IsSecurePortEnabled = instanceConfig.SecurePortEnabled;
        info.VipAddress = instanceConfig.VirtualHostName;
        info.SecureVipAddress = instanceConfig.SecureVirtualHostName;
        info.HomePageUrl = MakeUrl(info, instanceConfig.HomePageUrlPath, instanceConfig.HomePageUrl);
        info.StatusPageUrl = MakeUrl(info, instanceConfig.StatusPageUrlPath, instanceConfig.StatusPageUrl);
        info.AsgName = instanceConfig.AsgName;
        info.HealthCheckUrl = MakeUrl(info, instanceConfig.HealthCheckUrlPath, instanceConfig.HealthCheckUrl, instanceConfig.SecureHealthCheckUrl);

        if (!instanceConfig.IsInstanceEnabledOnInit)
        {
            info._status = InstanceStatus.Starting;
        }

        if (!string.IsNullOrEmpty(info.InstanceId))
        {
            InstanceInfo me = ApplicationInfoManager.Instance.InstanceInfo;

            if (me != null && info.InstanceId.Equals(me.InstanceId))
            {
                info.IsCoordinatingDiscoveryServer = true;
            }
        }

        if (instanceConfig.MetadataMap != null && instanceConfig.MetadataMap.Count > 0)
        {
            info._metaData = new Dictionary<string, string>(instanceConfig.MetadataMap);
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
            info.IpAddress = json.IpAddress;
            info.Port = json.Port?.Port ?? 0;
            info.IsInsecurePortEnabled = json.Port != null && json.Port.Enabled;
            info.SecurePort = json.SecurePort?.Port ?? 0;
            info.IsSecurePortEnabled = json.SecurePort != null && json.SecurePort.Enabled;
            info.HomePageUrl = json.HomePageUrl;
            info.StatusPageUrl = json.StatusPageUrl;
            info.HealthCheckUrl = json.HealthCheckUrl;
            info.SecureHealthCheckUrl = json.SecureHealthCheckUrl;
            info.VipAddress = json.VipAddress;
            info.SecureVipAddress = json.SecureVipAddress;
            info.CountryId = json.CountryId;
            info.DataCenterInfo = json.DataCenterInfo == null ? null : AppInfo.DataCenterInfo.FromJson(json.DataCenterInfo);
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
        var instanceInfo = new JsonInstanceInfo
        {
            InstanceId = InstanceId,
            Sid = Sid ?? "na",
            AppName = AppName,
            AppGroupName = AppGroupName,
            IpAddress = IpAddress,
            Port = new JsonInstanceInfo.JsonPortWrapper(IsInsecurePortEnabled, Port),
            SecurePort = new JsonInstanceInfo.JsonPortWrapper(IsSecurePortEnabled, SecurePort),
            HomePageUrl = HomePageUrl,
            StatusPageUrl = StatusPageUrl,
            HealthCheckUrl = HealthCheckUrl,
            SecureHealthCheckUrl = SecureHealthCheckUrl,
            VipAddress = VipAddress,
            SecureVipAddress = SecureVipAddress,
            CountryId = CountryId,
            DataCenterInfo = ((DataCenterInfo)DataCenterInfo)?.ToJson(),
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

        return instanceInfo;
    }

    private static Dictionary<string, string> GetMetaDataFromJson(Dictionary<string, string> json)
    {
        if (json == null)
        {
            return new Dictionary<string, string>();
        }

        if (json.TryGetValue("@class", out string value) && value.Equals("java.util.Collections$EmptyMap"))
        {
            return new Dictionary<string, string>();
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
