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

using Steeltoe.Discovery.Eureka.Transport;
using Steeltoe.Discovery.Eureka.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Discovery.Eureka.AppInfo
{
    public class InstanceInfo
    {
        public string InstanceId { get; internal set; }

        public string AppName { get; internal set; }

        public string AppGroupName { get; internal set; }

        public string IpAddr { get; internal set; }

        private string _sid;

        public string Sid
        {
            get
            {
                return _sid;
            }

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

        private InstanceStatus _status;

        public InstanceStatus Status
        {
            get
            {
                return _status;
            }

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

        private Dictionary<string, string> _metaData;

        public Dictionary<string, string> Metadata
        {
            get
            {
                return _metaData;
            }

            internal set
            {
                _metaData = value;
                IsDirty = true;
            }
        }

        public long LastUpdatedTimestamp { get; internal set; }

        public long LastDirtyTimestamp { get; internal set; }

        public ActionType Actiontype { get; internal set; }

        public string AsgName { get; internal set; }

        public bool IsUnsecurePortEnabled { get; internal set; }

        public bool IsSecurePortEnabled { get; internal set; }

        private bool _isDirty;

        public bool IsDirty
        {
            get
            {
                return _isDirty;
            }

            internal set
            {
                if (value)
                {
                    LastDirtyTimestamp = DateTime.UtcNow.Ticks;
                }

                _isDirty = value;
            }
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj == null)
            {
                return false;
            }

            if (!(obj is InstanceInfo other))
            {
                return false;
            }

            if (other.InstanceId.Equals(InstanceId))
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            var result = (31 * 1) + ((InstanceId == null) ? 0 : InstanceId.GetHashCode());
            return result;
        }

        public override string ToString()
        {
            var sb = new StringBuilder("Instance[");
            sb.Append("InstanceId=" + InstanceId);
            sb.Append(",");
            sb.Append("HostName=" + HostName);
            sb.Append(",");
            sb.Append("IpAddr=" + IpAddr);
            sb.Append(",");
            sb.Append("Status=" + Status.ToString());
            sb.Append(",");
            sb.Append("IsUnsecurePortEnabled=" + IsUnsecurePortEnabled);
            sb.Append(",");
            sb.Append("Port=" + Port);
            sb.Append(",");
            sb.Append("IsSecurePortEnabled=" + IsSecurePortEnabled);
            sb.Append(",");
            sb.Append("SecurePort=" + SecurePort);
            sb.Append(",");
            sb.Append("VipAddress=" + VipAddress);
            sb.Append(",");
            sb.Append("SecureVipAddress=" + SecureVipAddress);
            sb.Append(",");
            sb.Append("ActionType=" + Actiontype.ToString());
            sb.Append("]");
            return sb.ToString();
        }

        internal InstanceInfo()
        {
            OverriddenStatus = InstanceStatus.UNKNOWN;
            IsSecurePortEnabled = false;
            IsCoordinatingDiscoveryServer = false;
            IsUnsecurePortEnabled = true;
            CountryId = 1;
            Port = 7001;
            SecurePort = 7002;
            LastUpdatedTimestamp = LastDirtyTimestamp = DateTime.UtcNow.Ticks;
            _sid = "na";
            _metaData = new Dictionary<string, string>();
            _isDirty = false;
            _status = InstanceStatus.UP;
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

            var defaultAddress = instanceConfig.GetHostName(false);
            if (instanceConfig.PreferIpAddress || string.IsNullOrEmpty(defaultAddress))
            {
                defaultAddress = instanceConfig.IpAddress;
            }

            info.AppName = instanceConfig.AppName.ToUpperInvariant();
            info.AppGroupName = instanceConfig.AppGroupName?.ToUpperInvariant();
            info.DataCenterInfo = instanceConfig.DataCenterInfo;
            info.IpAddr = instanceConfig.IpAddress;
            info.HostName = defaultAddress;
            info.Port = (instanceConfig.NonSecurePort == -1) ? EurekaInstanceConfig.Default_NonSecurePort : instanceConfig.NonSecurePort;
            info.IsUnsecurePortEnabled = instanceConfig.IsNonSecurePortEnabled;
            info.SecurePort = (instanceConfig.SecurePort == -1) ? EurekaInstanceConfig.Default_SecurePort : instanceConfig.SecurePort;
            info.IsSecurePortEnabled = instanceConfig.SecurePortEnabled;
            info.VipAddress = instanceConfig.VirtualHostName;
            info.SecureVipAddress = instanceConfig.SecureVirtualHostName;
            info.HomePageUrl = MakeUrl(info, instanceConfig.HomePageUrlPath, instanceConfig.HomePageUrl);
            info.StatusPageUrl = MakeUrl(info, instanceConfig.StatusPageUrlPath, instanceConfig.StatusPageUrl);
            info.AsgName = instanceConfig.ASGName;
            info.HealthCheckUrl = MakeUrl(info, instanceConfig.HealthCheckUrlPath, instanceConfig.HealthCheckUrl, instanceConfig.SecureHealthCheckUrl);

            if (!instanceConfig.IsInstanceEnabledOnInit)
            {
                info._status = InstanceStatus.STARTING;
            }

            if (!string.IsNullOrEmpty(info.InstanceId))
            {
                var me = ApplicationInfoManager.Instance.InstanceInfo;
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
                info.IpAddr = json.IpAddr;
                info.Port = (json.Port == null) ? 0 : json.Port.Port;
                info.IsUnsecurePortEnabled = (json.Port == null) ? false : json.Port.Enabled;
                info.SecurePort = (json.SecurePort == null) ? 0 : json.SecurePort.Port;
                info.IsSecurePortEnabled = (json.SecurePort == null) ? false : json.SecurePort.Enabled;
                info.HomePageUrl = json.HomePageUrl;
                info.StatusPageUrl = json.StatusPageUrl;
                info.HealthCheckUrl = json.HealthCheckUrl;
                info.SecureHealthCheckUrl = json.SecureHealthCheckUrl;
                info.VipAddress = json.VipAddress;
                info.SecureVipAddress = json.SecureVipAddress;
                info.CountryId = json.CountryId;
                info.DataCenterInfo = (json.DataCenterInfo == null) ? null : AppInfo.DataCenterInfo.FromJson(json.DataCenterInfo);
                info.HostName = json.HostName;
                info.Status = json.Status;
                info.OverriddenStatus = json.OverriddenStatus;
                info.LeaseInfo = LeaseInfo.FromJson(json.LeaseInfo);
                info.IsCoordinatingDiscoveryServer = json.IsCoordinatingDiscoveryServer;
                info.LastUpdatedTimestamp = DateTimeConversions.FromJavaMillis(json.LastUpdatedTimestamp).Ticks;
                info.LastDirtyTimestamp = DateTimeConversions.FromJavaMillis(json.LastDirtyTimestamp).Ticks;
                info.Actiontype = json.Actiontype;
                info.AsgName = json.AsgName;
                info._metaData = GetMetaDataFromJson(json.Metadata);
                info.InstanceId = GetInstanceIdFromJson(json, info._metaData);
            }

            return info;
        }

        internal JsonInstanceInfo ToJsonInstance()
        {
            var jinfo = new JsonInstanceInfo
            {
                InstanceId = InstanceId,
                Sid = Sid ?? "na",
                AppName = AppName,
                AppGroupName = AppGroupName,
                IpAddr = IpAddr,
                Port = new JsonInstanceInfo.JsonPortWrapper(IsUnsecurePortEnabled, Port),
                SecurePort = new JsonInstanceInfo.JsonPortWrapper(IsSecurePortEnabled, SecurePort),
                HomePageUrl = HomePageUrl,
                StatusPageUrl = StatusPageUrl,
                HealthCheckUrl = HealthCheckUrl,
                SecureHealthCheckUrl = SecureHealthCheckUrl,
                VipAddress = VipAddress,
                SecureVipAddress = SecureVipAddress,
                CountryId = CountryId,
                DataCenterInfo = (DataCenterInfo == null) ? null : ((DataCenterInfo)DataCenterInfo).ToJson(),
                HostName = HostName,
                Status = Status,
                OverriddenStatus = OverriddenStatus,
                LeaseInfo = LeaseInfo?.ToJson(),
                IsCoordinatingDiscoveryServer = IsCoordinatingDiscoveryServer,
                LastUpdatedTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(LastUpdatedTimestamp, DateTimeKind.Utc)),
                LastDirtyTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(LastDirtyTimestamp, DateTimeKind.Utc)),
                Actiontype = Actiontype,
                AsgName = AsgName,
                Metadata = (Metadata.Count == 0) ? new Dictionary<string, string>() { { "@class", "java.util.Collections$EmptyMap" } } : Metadata
            };

            return jinfo;
        }

        private static Dictionary<string, string> GetMetaDataFromJson(Dictionary<string, string> json)
        {
            if (json == null)
            {
                return new Dictionary<string, string>();
            }

            if (json.TryGetValue("@class", out var value) && value.Equals("java.util.Collections$EmptyMap"))
            {
                return new Dictionary<string, string>();
            }

            return new Dictionary<string, string>(json);
        }

        private static string GetInstanceIdFromJson(JsonInstanceInfo jinfo, Dictionary<string, string> metaData)
        {
            if (string.IsNullOrEmpty(jinfo.InstanceId))
            {
                if (metaData == null)
                {
                    return null;
                }

                if (metaData.TryGetValue("instanceId", out var mid))
                {
                    return jinfo.HostName + ":" + mid;
                }

                return null;
            }
            else
            {
                return jinfo.InstanceId;
            }
        }

        private static string MakeUrl(InstanceInfo info, string relativeUrl, string explicitUrl, string secureExplicitUrl = null)
        {
            if (!string.IsNullOrEmpty(explicitUrl))
            {
                return explicitUrl;
            }
            else if (!string.IsNullOrEmpty(relativeUrl) && info.IsUnsecurePortEnabled)
            {
                return "http://" + info.HostName + ":" + info.Port + relativeUrl;
            }

            if (!string.IsNullOrEmpty(secureExplicitUrl))
            {
                return secureExplicitUrl;
            }
            else if (info.IsSecurePortEnabled)
            {
                return "https://" + info.HostName + ":" + info.SecurePort + relativeUrl;
            }

            return string.Empty;
        }
    }

    public enum InstanceStatus
    {
        UP,
        DOWN,
        STARTING,
        OUT_OF_SERVICE,
        UNKNOWN
    }

    public enum ActionType
    {
        ADDED,
        MODIFIED,
        DELETED
    }
}
