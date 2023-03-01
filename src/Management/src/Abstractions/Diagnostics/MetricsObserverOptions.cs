// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Diagnostics;

public class MetricsObserverOptions //: IMetricsObserverOptions
{
    /// <inheritdoc />
    public string IngressIgnorePattern { get; set; }

    /// <inheritdoc />
    public string EgressIgnorePattern { get; set; }

    public bool AspNetCoreHosting { get; set; } = true;

    public bool GCEvents { get; set; } = true;

    public bool ThreadPoolEvents { get; set; } = true;

    public bool EventCounterEvents { get; set; }

    public bool HttpClientCore { get; set; }

    public bool HttpClientDesktop { get; set; }

    /// <inheritdoc />
    public List<string> IncludedMetrics { get; set; } = new();

    /// <inheritdoc />
    public List<string> ExcludedMetrics { get; set; } = new();

    public bool IncludeObserver(string name)
    {
        switch (name)
        {
            case "AspnetCoreHostingObserver":
                return AspNetCoreHosting;
            case "HttpClientCoreObserver":
                return HttpClientCore;
            case "HttpClientDesktopObserver":
                return HttpClientDesktop;
            default: return true;
        }
    }
}
