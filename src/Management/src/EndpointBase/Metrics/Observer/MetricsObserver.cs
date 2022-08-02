// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Diagnostics;

namespace Steeltoe.Management.Endpoint.Metrics.Observer;

public abstract class MetricsObserver : DiagnosticObserver
{
    private Regex _pathMatcher;

    protected IMetricsObserverOptions Options { get; }

    protected MetricsObserver(string observerName, string diagnosticName, IMetricsObserverOptions options, ILogger logger = null)
        : base(observerName, diagnosticName, logger)
    {
        Options = options;
    }

    protected Regex GetPathMatcher()
    {
        return _pathMatcher;
    }

    protected void SetPathMatcher(Regex value)
    {
        _pathMatcher = value;
    }

    public abstract override void ProcessEvent(string eventName, object value);

    protected internal double MillisecondsToSeconds(double totalMilliseconds)
    {
        return totalMilliseconds / 1000;
    }

    protected internal virtual bool ShouldIgnoreRequest(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        return GetPathMatcher().IsMatch(path);
    }
}
