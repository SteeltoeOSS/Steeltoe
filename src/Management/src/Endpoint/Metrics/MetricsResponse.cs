// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Metrics;

public sealed class MetricsResponse
{
    [JsonPropertyName("name")]
    [JsonPropertyOrder(1)]
    public string? Name { get; }

    [JsonPropertyName("measurements")]
    [JsonPropertyOrder(2)]
    public IList<MetricSample>? Measurements { get; }

    [JsonPropertyOrder(3)]
    [JsonPropertyName("availableTags")]
    public IList<MetricTag>? AvailableTags { get; }

    [JsonPropertyName("names")]
    [JsonPropertyOrder(0)]
    public ISet<string>? Names { get; }

    public MetricsResponse(ISet<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);
        ArgumentGuard.ElementsNotNullOrEmpty(names);

        Names = names;
    }

    public MetricsResponse(string name, IList<MetricSample> measurements, IList<MetricTag> availableTags)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(measurements);
        ArgumentGuard.ElementsNotNull(measurements);
        ArgumentNullException.ThrowIfNull(availableTags);
        ArgumentGuard.ElementsNotNull(availableTags);

        Name = name;
        Measurements = measurements;
        AvailableTags = availableTags;
    }
}
