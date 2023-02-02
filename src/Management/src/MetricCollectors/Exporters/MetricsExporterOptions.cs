// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.MetricCollectors.Exporters;

namespace Steeltoe.Management.MetricCollectors.Exporters;

internal sealed class MetricsExporterOptions : IExporterOptions
{
    public int CacheDurationMilliseconds { get; set; }
    public int MaxTimeSeries { get; set; }
    public int MaxHistograms { get; set; }
    public List<KeyValuePair<string, string>>? IncludedMetrics { get;set; }
    public int MetricsCacheDurationMilliseconds { get; internal set; }
}
