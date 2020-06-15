// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.OpenTelemetry.Stats;
using System.Text.RegularExpressions;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    public abstract class MetricsObserver : DiagnosticObserver
    {
        protected Meter Meter { get; }

        protected IMetricsObserverOptions Options { get; }

        protected Regex PathMatcher { get; set; }

        public MetricsObserver(string observerName, string diagnosticName, IMetricsObserverOptions options, IStats stats, ILogger logger = null)
            : base(observerName, diagnosticName, logger)
        {
            Meter = stats.Meter;
            Options = options;
        }

        public abstract override void ProcessEvent(string evnt, object arg);

        protected internal double MilliToSeconds(double totalMilliseconds)
        {
            return totalMilliseconds / 1000;
        }

        protected internal virtual bool ShouldIgnoreRequest(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            return PathMatcher.IsMatch(path);
        }
    }
}
