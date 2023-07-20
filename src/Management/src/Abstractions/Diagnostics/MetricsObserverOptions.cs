// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#pragma warning disable S4004 // Collection properties should be readonly

namespace Steeltoe.Management.Diagnostics;

public sealed class MetricsObserverOptions
{
    /// <summary>
    /// Gets or sets a regex pattern for requests coming into this application where metrics should not be captured.
    /// </summary>
    public string IngressIgnorePattern { get; set; }

    /// <summary>
    /// Gets or sets a regex pattern for requests leaving this application where metrics should not be captured.
    /// </summary>
    public string EgressIgnorePattern { get; set; }

    public bool AspNetCoreHosting { get; set; } = true;

    public bool GCEvents { get; set; } = true;

    public bool ThreadPoolEvents { get; set; } = true;

    public bool EventCounterEvents { get; set; }

    public bool HttpClientCore { get; set; }

    public bool HttpClientDesktop { get; set; }

    /// <summary>
    /// Gets or sets an allow list of metrics that should be captured.
    /// </summary>
    /// <remarks>
    /// Currently only applies to System.Runtime metrics captured by "EventCounterListener".
    /// <para />
    /// See this list for values to choose from: <see href="https://docs.microsoft.com/dotnet/core/diagnostics/available-counters#systemruntime-counters" />.
    /// </remarks>
    public IList<string> IncludedMetrics { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets a list of metrics that should not be captured. Entries in <see cref="IncludedMetrics" /> take precedence in case of conflict.
    /// </summary>
    /// <remarks>
    /// Currently only applies to System.Runtime metrics captured by EventCounterListener.
    /// <para />
    /// See this list for values to choose from: <see href="https://docs.microsoft.com/dotnet/core/diagnostics/available-counters#systemruntime-counters" />.
    /// </remarks>
    public IList<string> ExcludedMetrics { get; set; } = new List<string>();

    public bool IncludeObserver(string name)
    {
        return name switch
        {
            "AspnetCoreHostingObserver" => AspNetCoreHosting,
            "HttpClientCoreObserver" => HttpClientCore,
            "HttpClientDesktopObserver" => HttpClientDesktop,
            _ => true
        };
    }
}
