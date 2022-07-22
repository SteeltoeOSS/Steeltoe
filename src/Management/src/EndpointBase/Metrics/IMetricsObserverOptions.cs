// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Metrics.Observer;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Metrics;

public interface IMetricsObserverOptions
{
    /// <summary>
    /// Gets a regex pattern for requests coming into this application where metrics should not be captured
    /// </summary>
    string IngressIgnorePattern { get; }

    /// <summary>
    /// Gets a regex pattern for requests leaving this application where metrics should not be captured
    /// </summary>
    string EgressIgnorePattern { get; }

    /// <summary>
    /// Gets a list of metrics that should not be captured
    /// </summary>
    /// <remarks>
    ///     Currently only applies to System.Runtime metrics captured by <see cref="EventCounterListener"/><para />
    ///     See this list for values to choose from: <see href="https://docs.microsoft.com/dotnet/core/diagnostics/available-counters#systemruntime-counters" />
    /// </remarks>
    List<string> ExcludedMetrics { get; }
}