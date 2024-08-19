// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Diagnostics;

public sealed class MetricsObserverOptions
{
    /// <summary>
    /// Gets or sets a regex pattern for requests coming into this application where metrics should not be captured.
    /// </summary>
    public string? IngressIgnorePattern { get; set; }

    /// <summary>
    /// Gets or sets a regex pattern for requests leaving this application where metrics should not be captured.
    /// </summary>
    public string? EgressIgnorePattern { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable the Steeltoe ASP.NET Core hosting observer (HTTP server metrics). Default value: true.
    /// </summary>
    public bool AspNetCoreHosting { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable the Steeltoe HttpClient observer for ASP.NET Core. Default value: false.
    /// </summary>
    public bool HttpClientCore { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable the Steeltoe HttpClient observer for desktop apps. Default value: false.
    /// </summary>
    public bool HttpClientDesktop { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable metrics for CLR garbage collection. Default value: true.
    /// </summary>
    public bool GCEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable metrics for CLR thread pool events. Default value: true.
    /// </summary>
    public bool ThreadPoolEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable metrics for EventCounter events. Default value: false.
    /// </summary>
    public bool EventCounterEvents { get; set; }

    /// <summary>
    /// Gets or sets how often to export, in seconds, when <see cref="EventCounterEvents" /> is set to <c>true</c>. Default value: 1.
    /// </summary>
    public int? EventCounterIntervalSec { get; set; } = 1;

    /// <summary>
    /// Gets a list of metrics that should be captured. This takes precedence over <see cref="ExcludedMetrics" /> in case of conflict.
    /// </summary>
    /// <remarks>
    /// Currently only applies to System.Runtime metrics captured by EventCounterListener.
    /// <para />
    /// See this list for values to choose from: <see href="https://docs.microsoft.com/dotnet/core/diagnostics/available-counters#systemruntime-counters" />.
    /// </remarks>
    public IList<string> IncludedMetrics { get; } = new List<string>();

    /// <summary>
    /// Gets a list of metrics that should not be captured. Entries in <see cref="IncludedMetrics" /> take precedence in case of conflict.
    /// </summary>
    /// <remarks>
    /// Currently only applies to System.Runtime metrics captured by EventCounterListener.
    /// <para />
    /// See this list for values to choose from: <see href="https://docs.microsoft.com/dotnet/core/diagnostics/available-counters#systemruntime-counters" />.
    /// </remarks>
    public IList<string> ExcludedMetrics { get; } = new List<string>();

    internal bool IncludesObserver(string name)
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
