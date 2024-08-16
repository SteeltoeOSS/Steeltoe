// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.Metrics;

internal sealed class MetricsExporterOptions
{
    public int CacheDurationMilliseconds { get; set; }
    public int MaxTimeSeries { get; set; }
    public int MaxHistograms { get; set; }
    public IList<string> IncludedMetrics { get; set; } = new List<string>();
}
