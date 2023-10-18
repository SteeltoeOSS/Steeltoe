// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Diagnostics;

namespace Steeltoe.Management.Endpoint.Metrics.Observer;

internal abstract class MetricsObserver : DiagnosticObserver
{
    private Regex? _pathMatcher;

    protected MetricsObserver(string observerName, string diagnosticName, ILoggerFactory loggerFactory)
        : base(observerName, diagnosticName, loggerFactory)
    {
    }

    protected void SetPathMatcher(Regex value)
    {
        _pathMatcher = value;
    }

    public bool ShouldIgnoreRequest(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        return _pathMatcher != null && _pathMatcher.IsMatch(path);
    }
}
