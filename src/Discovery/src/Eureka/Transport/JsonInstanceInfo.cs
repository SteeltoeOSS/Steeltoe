// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka.Transport;

internal sealed class JsonInstanceInfo
{
    // All fields are nullable, so the client won't send unspecified values using
    // JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull.

    [JsonPropertyName("instanceId")]
    public string? InstanceId { get; set; }

    [JsonPropertyName("app")]
    public string? AppName { get; set; }

    [JsonPropertyName("appGroupName")]
    public string? AppGroupName { get; set; }

    [JsonPropertyName("ipAddr")]
    public string? IPAddress { get; set; }

    [JsonPropertyName("sid")]
    public string? Sid { get; set; }

    [JsonPropertyName("port")]
    public JsonPortWrapper? Port { get; set; }

    [JsonPropertyName("securePort")]
    public JsonPortWrapper? SecurePort { get; set; }

    [JsonPropertyName("homePageUrl")]
    public string? HomePageUrl { get; set; }

    [JsonPropertyName("statusPageUrl")]
    public string? StatusPageUrl { get; set; }

    [JsonPropertyName("healthCheckUrl")]
    public string? HealthCheckUrl { get; set; }

    [JsonPropertyName("secureHealthCheckUrl")]
    public string? SecureHealthCheckUrl { get; set; }

    [JsonPropertyName("vipAddress")]
    public string? VipAddress { get; set; }

    [JsonPropertyName("secureVipAddress")]
    public string? SecureVipAddress { get; set; }

    [JsonPropertyName("countryId")]
    public int? CountryId { get; set; }

    [JsonPropertyName("dataCenterInfo")]
    public JsonDataCenterInfo? DataCenterInfo { get; set; }

    [JsonPropertyName("hostName")]
    public string? HostName { get; set; }

    [JsonPropertyName("status")]
    public InstanceStatus? Status { get; set; }

    [JsonPropertyName("overriddenStatus")]
    public InstanceStatus? OverriddenStatus { get; set; }

    [JsonPropertyName("overriddenstatus")]
    public InstanceStatus? OverriddenStatusLegacy { get; set; }

    [JsonPropertyName("leaseInfo")]
    public JsonLeaseInfo? LeaseInfo { get; set; }

    [JsonPropertyName("isCoordinatingDiscoveryServer")]
    [JsonConverter(typeof(BoolStringJsonConverter))]
    public bool? IsCoordinatingDiscoveryServer { get; set; }

    [JsonPropertyName("metadata")]
    public IDictionary<string, string?>? Metadata { get; set; }

    [JsonPropertyName("lastUpdatedTimestamp")]
    [JsonConverter(typeof(LongStringJsonConverter))]
    public long? LastUpdatedTimestamp { get; set; }

    [JsonPropertyName("lastDirtyTimestamp")]
    [JsonConverter(typeof(LongStringJsonConverter))]
    public long? LastDirtyTimestamp { get; set; }

    [JsonPropertyName("actionType")]
    public ActionType? ActionType { get; set; }

    [JsonPropertyName("asgName")]
    public string? AutoScalingGroupName { get; set; }
}
