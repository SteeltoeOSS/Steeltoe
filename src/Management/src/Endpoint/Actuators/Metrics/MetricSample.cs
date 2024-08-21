// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Actuators.Metrics;

public sealed class MetricSample
{
    [JsonPropertyName("statistic")]
    public MetricStatistic Statistic { get; }

    [JsonPropertyName("value")]
    public double Value { get; }

    [JsonIgnore]
    public IList<KeyValuePair<string, string>>? Tags { get; }

    public MetricSample(MetricStatistic statistic, double value, IList<KeyValuePair<string, string>>? tags)
    {
        Statistic = statistic;
        Value = value;
        Tags = tags;
    }

    public override string ToString()
    {
        return $"MeasurementSample{{Statistic={Statistic}, Value={Value}}}";
    }
}
