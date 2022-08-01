// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.OpenTelemetry.Metrics;

namespace Steeltoe.Management.OpenTelemetry.Exporters;

public readonly struct SteeltoeCollectionResponse : ICollectionResponse
{
    public SteeltoeCollectionResponse(MetricsCollection<List<MetricSample>> metricSamples, MetricsCollection<List<MetricTag>> availableTags, DateTime generatedAtUtc)
    {
        MetricSamples = metricSamples;
        AvailableTags = availableTags;
        GeneratedAtUtc = generatedAtUtc;
    }

    public MetricsCollection<List<MetricSample>> MetricSamples { get; }

    public MetricsCollection<List<MetricTag>> AvailableTags { get; }

    public DateTime GeneratedAtUtc { get; }
}
