// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.OpenTelemetry.Metrics;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Metrics;

public class MetricsResponse : IMetricsResponse
{
    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("measurements")]
    public List<MetricSample> Measurements { get; }

    [JsonPropertyName("availableTags")]
    public List<MetricTag> AvailableTags { get; }

    public MetricsResponse(string name, List<MetricSample> measurements, List<MetricTag> availableTags)
    {
        Name = name;
        Measurements = measurements;
        AvailableTags = availableTags;
    }
}
