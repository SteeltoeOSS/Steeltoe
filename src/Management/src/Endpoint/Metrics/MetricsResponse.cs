// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Steeltoe.Management.MetricCollectors;

namespace Steeltoe.Management.Endpoint.Metrics;

public class MetricsResponse : IMetricsResponse
{
    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("measurements")]
    public IList<MetricSample> Measurements { get; }

    [JsonPropertyName("availableTags")]
    public IList<MetricTag> AvailableTags { get; }

    public MetricsResponse(string name, IList<MetricSample> measurements, IList<MetricTag> availableTags)
    {
        Name = name;
        Measurements = measurements;
        AvailableTags = availableTags;
    }
}
