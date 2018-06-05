// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
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

        public MetricsObserver(string observerName, string diagnosticName, IMetricsOptions options, IStats censusStats, ITags censusTags, ILogger logger)
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
