// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Metrics;
using Steeltoe.Management.MetricCollectors.Aggregations;
using Steeltoe.Management.MetricCollectors.Metrics;

namespace Steeltoe.Management.MetricCollectors.Exporters.Steeltoe;

public interface ISteeltoeExporter
{
    void SetCollect(Action collect);

    (MetricsCollection<IList<MetricSample>> MetricSamples, MetricsCollection<IList<MetricTag>> AvailableTags) Export();

    public void AddMetrics(Instrument instrument, LabeledAggregationStatistics stats);
}
