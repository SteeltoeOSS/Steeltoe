//
// Copyright 2015 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use ths file except in compliance with the License.
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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SteelToe.Discovery.Eureka.AppInfo;
using System.Collections.Generic;
using System.IO;

namespace SteelToe.Discovery.Eureka.Transport
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
                Enabled = enabled.ToString();
                Port = port;
            }
            [JsonProperty("@enabled")]
            public string Enabled { get; private set; }

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
