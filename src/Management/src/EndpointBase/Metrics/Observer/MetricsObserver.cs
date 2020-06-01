// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenCensus.Stats;
using OpenCensus.Tags;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Census.Stats;
using Steeltoe.Management.Census.Tags;
using System.Text.RegularExpressions;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    public abstract class MetricsObserver : DiagnosticObserver
    {
        protected IViewManager ViewManager { get; }

        protected IStatsRecorder StatsRecorder { get; }

        protected ITagger Tagger { get; }

        protected IMetricsOptions Options { get; }

        protected Regex PathMatcher { get; set; }

        public MetricsObserver(string observerName, string diagnosticName, IMetricsOptions options, IStats censusStats, ITags censusTags, ILogger logger = null)
            : base(observerName, diagnosticName, logger)
        {
            ViewManager = censusStats.ViewManager;
            StatsRecorder = censusStats.StatsRecorder;
            Tagger = censusTags.Tagger;
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
