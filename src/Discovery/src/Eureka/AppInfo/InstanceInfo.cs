// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Http.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Steeltoe.Discovery.Eureka.AppInfo
{
    public class InstanceInfo
    {
        public string InstanceId { get; set; }

        [JsonPropertyName("app")]
        public string AppName { get; set; }

        public string AppGroupName { get; set; }

        public string IpAddr { get; set; }

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

        [JsonPropertyName("port")]
        public InstancePort PortProperties
        {
            get
            {
                return _port;
            }

            set
            {
                _port = value;
                Port = value.Port;
            }
        }

        [JsonIgnore]
        public int Port { get; internal set; }

        private InstancePort _port;

        [JsonPropertyName("securePort")]
        public InstancePort SecurePortProperties
        {
            get
            {
                return _securePort;
            }

            set
            {
                _securePort = value;
                SecurePort = value.Port;
            }
        }

        [JsonIgnore]
        public int SecurePort { get; internal set; }

        private InstancePort _securePort;

        public string HomePageUrl { get; set; }

        public string StatusPageUrl { get; set; }

        public string HealthCheckUrl { get; set; }

        public string SecureHealthCheckUrl { get; set; }

        public string VipAddress { get; set; }

        public string SecureVipAddress { get; set; }

        public int CountryId { get; set; }

        [JsonPropertyName("dataCenterInfo")]
        public DataCenterInfoProperties DataCenterInfoProperties
        {
            get
            {
                return _dataCenterInfoProperties;
            }

            set
            {
                _dataCenterInfoProperties = value;
                DataCenterInfo = new DataCenterInfo(_dataCenterInfoProperties?.Name);
            }
        }

        // Deserialization of interfaces not supported by System.Text.Json
        [JsonIgnore]
        public IDataCenterInfo DataCenterInfo { get; internal set; }

        private DataCenterInfoProperties _dataCenterInfoProperties;

        public string HostName { get; set; }

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

        public LeaseInfo LeaseInfo { get; set; }

        public bool IsCoordinatingDiscoveryServer { get; internal set; }

        private Dictionary<string, string> _metaData;

        public Dictionary<string, string> Metadata
        {
            get
            {
                return _metaData;
            }

            set
            {
                _metaData = value;
                IsDirty = true;
            }
        }

        [JsonConverter(typeof(LongStringJsonConverter))]
        public long LastUpdatedTimestamp { get; set; }

        [JsonConverter(typeof(LongStringJsonConverter))]
        public long LastDirtyTimestamp { get; set; }

        [JsonIgnore]
        public ActionType Actiontype { get; set; }

        public string AsgName { get; set; }

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

        public InstanceInfo()
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

    public class DataCenterInfoProperties
    {
        [JsonPropertyName("@class")]
        public string ClassName { get; set; }

        public string Name { get; set; }
    }

    public class InstancePort
    {
        [JsonPropertyName("@enabled")]
        [JsonConverter(typeof(BoolStringJsonConverter))]
        public bool Enabled { get; set; }

        [JsonPropertyName("$")]
        public int Port { get; set; }
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
