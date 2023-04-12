// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Metrics;

namespace Steeltoe.Management.MetricCollectors.Exporters.Steeltoe;

public interface ISteeltoeExporter
{
    Action? Collect { get; set; }

    (MetricsCollection<List<MetricSample>> MetricSamples, MetricsCollection<List<MetricTag>> AvailableTags) Export();

    public void AddMetrics(Instrument instrument, LabeledAggregationStatistics stats);
}
