// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Steeltoe.Discovery.Eureka.AppInfo;
using System.Collections.Generic;
using System.IO;

namespace Steeltoe.Discovery.Eureka.Transport
{
    internal class JsonInstanceInfo
    {
        [JsonProperty("instanceId")]
        public string InstanceId { get; set; }

        [JsonProperty("app")]
        public string AppName { get; set; }

        [JsonProperty("appGroupName")]
        public string AppGroupName { get; set; }

        [JsonProperty("ipAddr")]
        public string IpAddr { get; set; }

        [JsonProperty("sid")]
        public string Sid { get; set; }

        [JsonProperty("port")]
        public JsonPortWrapper Port { get; set; }

        [JsonProperty("securePort")]
        public JsonPortWrapper SecurePort { get; set; }

        [JsonProperty("homePageUrl")]
        public string HomePageUrl { get; set; }

        [JsonProperty("statusPageUrl")]
        public string StatusPageUrl { get; set; }

        [JsonProperty("healthCheckUrl")]
        public string HealthCheckUrl { get; set; }

        [JsonProperty("secureHealthCheckUrl")]
        public string SecureHealthCheckUrl { get; set; }

        [JsonProperty("vipAddress")]
        public string VipAddress { get; set; }

        [JsonProperty("secureVipAddress")]
        public string SecureVipAddress { get; set; }

        [JsonProperty("countryId")]
        public int CountryId { get; set; }

        [JsonProperty("dataCenterInfo")]
        public JsonDataCenterInfo DataCenterInfo { get; set; }

        [JsonProperty("hostName")]
        public string HostName { get; set; }

        [JsonProperty("status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public InstanceStatus Status { get; set; }

        [JsonProperty("overriddenstatus")]
        [JsonConverter(typeof(StringEnumConverter))]
        public InstanceStatus OverriddenStatus { get; set; }

        [JsonProperty("leaseInfo")]
        public JsonLeaseInfo LeaseInfo { get; set; }

        [JsonProperty("isCoordinatingDiscoveryServer")]
        public bool IsCoordinatingDiscoveryServer { get; set; }

        [JsonProperty("metadata")]
        public Dictionary<string, string> Metadata { get; set; }

        [JsonProperty("lastUpdatedTimestamp")]
        public long LastUpdatedTimestamp { get; set; }

        [JsonProperty("lastDirtyTimestamp")]
        public long LastDirtyTimestamp { get; set; }

        [JsonProperty("actionType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ActionType Actiontype { get; set; }

        [JsonProperty("asgName", NullValueHandling = NullValueHandling.Ignore)]
        public string AsgName { get; set; }

        public class JsonPortWrapper
        {
            [JsonConstructor]
            public JsonPortWrapper(
                    [JsonProperty("@enabled")] bool enabled,
                    [JsonProperty("$")] int port)
            {
                Enabled = enabled;
                Port = port;
            }

            [JsonProperty("@enabled")]
            public bool Enabled { get; private set; }

            [JsonProperty("$")]
            public int Port { get; private set; }
        }

        public class JsonDataCenterInfo
        {
            [JsonConstructor]
            public JsonDataCenterInfo(
                [JsonProperty("@class")] string className,
                [JsonProperty("name")] string name)
            {
                ClassName = className;
                Name = name;
            }

            [JsonProperty("@class")]
            public string ClassName { get; private set; }

            [JsonProperty("name")]
            public string Name { get; private set; }
        }

        internal static JsonInstanceInfo Deserialize(Stream stream)
        {
            var result = JsonSerialization.Deserialize<JsonInstanceInfo>(stream);
            return result;
        }
    }
}
