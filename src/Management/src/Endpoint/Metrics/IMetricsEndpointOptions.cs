// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Steeltoe.Management.Endpoint.Metrics;

public interface IMetricsEndpointOptions : IEndpointOptions
{
    int CacheDurationMilliseconds { get; }
    public int MaxTimeSeries { get; set; }
    public int MaxHistograms { get; set; }

    [SuppressMessage("Major Code Smell", "S4004:Collection properties should be readonly", Justification = "Allow in Options")]
    public IList<string> IncludedMetrics { get; set; }
}
