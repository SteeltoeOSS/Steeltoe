// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Steeltoe.Common.Http.Serialization;

namespace Steeltoe.Discovery.Eureka.Transport;

internal sealed class JsonLeaseInfo
{
    [JsonPropertyName("renewalIntervalInSecs")]
    public int RenewalIntervalInSecs { get; set; }

    [JsonPropertyName("durationInSecs")]
    public int DurationInSecs { get; set; }

    [JsonPropertyName("registrationTimestamp")]
    [JsonConverter(typeof(LongStringJsonConverter))]
    public long RegistrationTimestamp { get; set; }

    [JsonPropertyName("lastRenewalTimestamp")]
    [JsonConverter(typeof(LongStringJsonConverter))]
    public long LastRenewalTimestamp { get; set; }

    [JsonPropertyName("renewalTimestamp")]
    [JsonConverter(typeof(LongStringJsonConverter))]
    public long LastRenewalTimestampLegacy { get; set; }

    [JsonPropertyName("evictionTimestamp")]
    [JsonConverter(typeof(LongStringJsonConverter))]
    public long EvictionTimestamp { get; set; }

    [JsonPropertyName("serviceUpTimestamp")]
    [JsonConverter(typeof(LongStringJsonConverter))]
    public long ServiceUpTimestamp { get; set; }
}
