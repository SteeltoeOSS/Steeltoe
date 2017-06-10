//
// Copyright 2015 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using Steeltoe.Discovery.Eureka.Transport;
using Steeltoe.Discovery.Eureka.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Discovery.Eureka.AppInfo
{
    public class InstanceInfo
    {
        private string _instanceId;
        public string InstanceId
        {
            get
            {
                return _instanceId;
            }
            internal set
            {
                _instanceId = value;
            }
        }

        private string _appName;
        public string AppName
        {
            get
            {
                return _appName;
            }
            internal set
            {
                _appName = value;
            }
        }

        private string _appGroupName;
        public string AppGroupName
        {
            get
            {
                return _appGroupName;
            }
            internal set
            {
                _appGroupName = value;
            }
        }

        private string _ipAddr;
        public string IpAddr
        {
            get
            {
                return _ipAddr;
            }
            internal set
            {
                _ipAddr = value;
            }
        }

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

        private int _port;
        public int Port
        {
            get
            {
                return _port;
            }
            internal set
            {
                _port = value;
            }
        }

        private int _securePort;
        public int SecurePort
        {
            get
            {
                return _securePort;
            }
            internal set
            {
                _securePort = value;
            }
        }

        private string _homePageUrl;
        public string HomePageUrl
        {
            get
            {
                return _homePageUrl;
            }
            internal set
            {
                _homePageUrl = value;
            }
        }

        private string _statusPageUrl;
        public string StatusPageUrl
        {
            get
            {
                return _statusPageUrl;
            }
            internal set
            {
                _statusPageUrl = value;
            }
        }

        private string _healthCheckUrl;
        public string HealthCheckUrl
        {
            get
            {
                return _healthCheckUrl;
            }
            internal set
            {
                _healthCheckUrl = value;
            }
        }

        private string _secureHealthCheckUrl;
        public string SecureHealthCheckUrl
        {
            get
            {
                return _secureHealthCheckUrl;
            }
            internal set
            {
                _secureHealthCheckUrl = value;
            }
        }

        private string _vipAddress;
        public string VipAddress
        {
            get
            {
                return _vipAddress;
            }
            internal set
            {
                _vipAddress = value;
            }
        }

        private string _secureVipAddress;
        public string SecureVipAddress
        {
            get
            {
                return _secureVipAddress;
            }
            internal set
            {
                _secureVipAddress = value;
            }
        }

        private int _countryId;
        public int CountryId
        {
            get
            {
                return _countryId;
            }
            internal set
            {
                _countryId = value;
            }
        }

        private IDataCenterInfo _dataCenterInfo;
        public IDataCenterInfo DataCenterInfo
        {
            get
            {
                return _dataCenterInfo;
            }
            internal set
            {
                _dataCenterInfo = value;
            }
        }

        private string _hostName;
        public string HostName
        {
            get
            {
                return _hostName;
            }
            internal set
            {
                _hostName = value;
            }
        }

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

        private InstanceStatus _overRiddenStatus;
        public InstanceStatus OverriddenStatus
        {
            get
            {
                return _overRiddenStatus;
            }
            internal set
            {
                _overRiddenStatus = value;
            }
        }

        private LeaseInfo _leaseInfo;
        public LeaseInfo LeaseInfo
        {
            get
            {
                return _leaseInfo;
            }
            internal set
            {
                _leaseInfo = value;
            }
        }

        private bool _isCoordinatingDiscoveryServer;
        public bool IsCoordinatingDiscoveryServer
        {
            get
            {
                return _isCoordinatingDiscoveryServer;
            }
            internal set
            {
                _isCoordinatingDiscoveryServer = value;
            }

        }

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

        private long _lastUpdatedTimestamp;
        public long LastUpdatedTimestamp
        {
            get
            {
                return _lastUpdatedTimestamp;
            }
            internal set
            {
                _lastUpdatedTimestamp = value;
            }
        }

        private long _lastDirtyTimestamp;
        public long LastDirtyTimestamp
        {
            get
            {
                return _lastDirtyTimestamp;
            }
            internal set
            {
                _lastDirtyTimestamp = value;
            }
        }

        private ActionType _actionType;
        public ActionType Actiontype
        {
            get
            {
                return _actionType;
            }
            internal set
            {
                _actionType = value;
            }
        }

        private string _asgName;
        public string AsgName
        {
            get
            {
                return _asgName;
            }
            internal set
            {
                _asgName = value;
            }
        }

        private bool _isUnsecurePortEnabled;
        public bool IsUnsecurePortEnabled
        {
            get
            {
                return _isUnsecurePortEnabled;
            }
            internal set
            {
                _isUnsecurePortEnabled = value;
            }
        }

        private bool _isSecurePortEnabled;
        public bool IsSecurePortEnabled
        {
            get
            {
                return _isSecurePortEnabled;
            }
            internal set
            {
                _isSecurePortEnabled = value;
            }
        }

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
                return true;
            if (obj == null)
                return false;

            InstanceInfo other = obj as InstanceInfo;
            if (other == null)
                return false;
            if (other.InstanceId.Equals(this.InstanceId))
                return true;

            return false;

        }
        public override int GetHashCode()
        {
            var result = 31 * 1 + ((InstanceId == null) ? 0 : InstanceId.GetHashCode());
            return result;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("Instance[");
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
            _overRiddenStatus = InstanceStatus.UNKNOWN;
            _isSecurePortEnabled = false;
            _isCoordinatingDiscoveryServer = false;
            _isUnsecurePortEnabled = true;
            _countryId = 1;
            _port = 7001;
            _securePort = 7002;
            _sid = "na";
            _metaData = new Dictionary<string, string>();
            _isDirty = false;
            _lastUpdatedTimestamp = _lastDirtyTimestamp = DateTime.UtcNow.Ticks;
            _status = InstanceStatus.UP;

        }
        internal JsonInstanceInfo ToJsonInstance()
        {
            JsonInstanceInfo jinfo = new JsonInstanceInfo();
            jinfo.InstanceId = this.InstanceId;
            jinfo.Sid = (this.Sid == null) ? "na" : this.Sid;
            jinfo.AppName = this.AppName;
            jinfo.AppGroupName = this.AppGroupName;
            jinfo.IpAddr = this.IpAddr;
            jinfo.Port = new JsonInstanceInfo.JsonPortWrapper(this.IsUnsecurePortEnabled, this.Port);
            jinfo.SecurePort = new JsonInstanceInfo.JsonPortWrapper(this.IsSecurePortEnabled, this.SecurePort);
            jinfo.HomePageUrl = this.HomePageUrl;
            jinfo.StatusPageUrl = this.StatusPageUrl;
            jinfo.HealthCheckUrl = this.HealthCheckUrl;
            jinfo.SecureHealthCheckUrl = this.SecureHealthCheckUrl;
            jinfo.VipAddress = this.VipAddress;
            jinfo.SecureVipAddress = this.SecureVipAddress;
            jinfo.CountryId = this.CountryId;
            jinfo.DataCenterInfo = (this.DataCenterInfo == null) ? null : ((AppInfo.DataCenterInfo)DataCenterInfo).ToJson();
            jinfo.HostName = this.HostName;
            jinfo.Status = this.Status;
            jinfo.OverriddenStatus = this.OverriddenStatus;
            jinfo.LeaseInfo = (this.LeaseInfo == null) ? null : this.LeaseInfo.ToJson();
            jinfo.IsCoordinatingDiscoveryServer = this.IsCoordinatingDiscoveryServer;
            jinfo.LastUpdatedTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(this.LastUpdatedTimestamp, DateTimeKind.Utc));
            jinfo.LastDirtyTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(this.LastDirtyTimestamp, DateTimeKind.Utc));
            jinfo.Actiontype = this.Actiontype;
            jinfo.AsgName = this.AsgName;
            jinfo.Metadata = (this.Metadata.Count == 0) ? new Dictionary<string, string>() { { "@class", "java.util.Collections$EmptyMap" } } : this.Metadata;

            return jinfo;
        }
        internal static InstanceInfo FromJsonInstance(JsonInstanceInfo json)
        {
            InstanceInfo info = new InstanceInfo();
            if (json != null)
            {
                info._sid = (json.Sid == null) ? "na" : json.Sid;
                info._appName = json.AppName;
                info._appGroupName = json.AppGroupName;
                info._ipAddr = json.IpAddr;
                info._port = (json.Port == null) ? 0 : json.Port.Port;
                info._isUnsecurePortEnabled = (json.Port == null) ? false : json.Port.Enabled;
                info._securePort = (json.SecurePort == null) ? 0 : json.SecurePort.Port;
                info._isSecurePortEnabled = (json.SecurePort == null) ? false : json.SecurePort.Enabled;
                info._homePageUrl = json.HomePageUrl;
                info._statusPageUrl = json.StatusPageUrl;
                info._healthCheckUrl = json.HealthCheckUrl;
                info._secureHealthCheckUrl = json.SecureHealthCheckUrl;
                info._vipAddress = json.VipAddress;
                info._secureVipAddress = json.SecureVipAddress;
                info._countryId = json.CountryId;
                info._dataCenterInfo = (json.DataCenterInfo == null) ? null : AppInfo.DataCenterInfo.FromJson(json.DataCenterInfo);
                info._hostName = json.HostName;
                info._status = json.Status;
                info._overRiddenStatus = json.OverriddenStatus;
                info._leaseInfo = LeaseInfo.FromJson(json.LeaseInfo);
                info._isCoordinatingDiscoveryServer = json.IsCoordinatingDiscoveryServer;
                info._lastUpdatedTimestamp = DateTimeConversions.FromJavaMillis(json.LastUpdatedTimestamp).Ticks;
                info._lastDirtyTimestamp = DateTimeConversions.FromJavaMillis(json.LastDirtyTimestamp).Ticks;
                info._actionType = json.Actiontype;
                info._asgName = json.AsgName;
                info._metaData = GetMetaDataFromJson(json.Metadata);
                info._instanceId = GetInstanceIdFromJson(json, info._metaData);

            }
            return info;
        }

        private static Dictionary<string, string> GetMetaDataFromJson(Dictionary<string, string> json)
        {
            if (json == null)
            {
                return new Dictionary<string, string>();
            }
            string value = null;
            if (json.TryGetValue("@class", out value))
            {
                if (value.Equals("java.util.Collections$EmptyMap"))
                {
                    return new Dictionary<string, string>();
                }
            }
            return new Dictionary<string, string>(json);
        }
        private static string GetInstanceIdFromJson(JsonInstanceInfo jinfo, Dictionary<string,string> metaData)
        {
            if (string.IsNullOrEmpty(jinfo.InstanceId))
            {
  
                if (metaData == null)
                {
                    return null;
                }
                string mid = null;
                if (metaData.TryGetValue("instanceId", out mid))
                {
                    return jinfo.HostName + ":" + mid;
                }
                return null;

            } else
            {
                return jinfo.InstanceId;
            }
        }


        internal static InstanceInfo FromInstanceConfig(IEurekaInstanceConfig instanceConfig)
        {
            InstanceInfo info = new InstanceInfo();
            info._leaseInfo = LeaseInfo.FromConfig(instanceConfig);
            info._instanceId = instanceConfig.InstanceId;
            if (string.IsNullOrEmpty(info.InstanceId))
            {
                info._instanceId = instanceConfig.GetHostName(false);
            }

            string defaultAddress = instanceConfig.GetHostName(false);
            if (string.IsNullOrEmpty(defaultAddress))
            {
                defaultAddress = instanceConfig.IpAddress;
            }

            info._appName = instanceConfig.AppName.ToUpperInvariant();
            info._appGroupName = (instanceConfig.AppGroupName != null) ? instanceConfig.AppGroupName.ToUpperInvariant() : null;
            info._dataCenterInfo = instanceConfig.DataCenterInfo;
            info._ipAddr = instanceConfig.IpAddress;
            info._hostName = defaultAddress;
            info._port = instanceConfig.NonSecurePort;
            info._isUnsecurePortEnabled = instanceConfig.IsNonSecurePortEnabled;
            info._securePort = instanceConfig.SecurePort;
            info._isSecurePortEnabled = instanceConfig.SecurePortEnabled;
            info._vipAddress = instanceConfig.VirtualHostName;
            info._secureVipAddress = instanceConfig.SecureVirtualHostName;
            info._homePageUrl = MakeUrl(info, instanceConfig.HomePageUrlPath, instanceConfig.HomePageUrl);
            info._statusPageUrl = MakeUrl(info, instanceConfig.StatusPageUrlPath, instanceConfig.StatusPageUrl);
            info._asgName = instanceConfig.ASGName;
            info._healthCheckUrl = MakeUrl(info, instanceConfig.HealthCheckUrlPath, instanceConfig.HealthCheckUrl, instanceConfig.SecureHealthCheckUrl);

            if (!instanceConfig.IsInstanceEnabledOnInit)
            {
                info._status = InstanceStatus.STARTING;
            }

            if (!string.IsNullOrEmpty(info._instanceId))
            {
                InstanceInfo me = ApplicationInfoManager.Instance.InstanceInfo;
                if (me != null)
                {
                    if (info._instanceId.Equals(me.InstanceId))
                    {
                        info._isCoordinatingDiscoveryServer = true;
                    }
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

    public enum InstanceStatus
    {
        UP, DOWN, STARTING, OUT_OF_SERVICE, UNKNOWN
    }

    public enum ActionType
    {
        ADDED, MODIFIED, DELETED
    }
}
