// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Actuators.Metrics;

public sealed class MetricSample(MetricStatistic statistic, double value, IList<KeyValuePair<string, string>>? tags)
{
    [JsonPropertyName("statistic")]
    public MetricStatistic Statistic { get; } = statistic;

    [JsonPropertyName("value")]
    public double Value { get; } = value;

    [JsonIgnore]
    public IList<KeyValuePair<string, string>>? Tags { get; } = tags;

    public override string ToString()
    {
        return $"MeasurementSample{{Statistic={Statistic}, Value={Value}}}";
    }
}
