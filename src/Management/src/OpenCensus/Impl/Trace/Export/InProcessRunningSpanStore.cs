// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Management.Census.Utils;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class InProcessRunningSpanStore : RunningSpanStoreBase
    {
        private readonly ConcurrentIntrusiveList<SpanBase> runningSpans;

        public InProcessRunningSpanStore()
        {
            runningSpans = new ConcurrentIntrusiveList<SpanBase>();
        }

        public override IRunningSpanStoreSummary Summary
        {
            get
            {
                ICollection<SpanBase> allRunningSpans = runningSpans.Copy();
                Dictionary<string, int> numSpansPerName = new Dictionary<string, int>();
                foreach (var span in allRunningSpans)
                {
                    numSpansPerName.TryGetValue(span.Name, out int prevValue);
                    numSpansPerName[span.Name] = prevValue + 1;
                }

                Dictionary<string, IRunningPerSpanNameSummary> perSpanNameSummary = new Dictionary<string, IRunningPerSpanNameSummary>();
                foreach (var it in numSpansPerName)
                {
                    int numRunningSpans = it.Value;
                    var runningPerSpanNameSummary = RunningPerSpanNameSummary.Create(numRunningSpans);
                    perSpanNameSummary[it.Key] = runningPerSpanNameSummary;
                }

                IRunningSpanStoreSummary summary = RunningSpanStoreSummary.Create(perSpanNameSummary);
                return summary;
            }
        }

        public override IList<ISpanData> GetRunningSpans(IRunningSpanStoreFilter filter)
        {
            ICollection<SpanBase> allRunningSpans = runningSpans.Copy();
            int maxSpansToReturn = filter.MaxSpansToReturn == 0 ? allRunningSpans.Count : filter.MaxSpansToReturn;
            List<ISpanData> ret = new List<ISpanData>(maxSpansToReturn);
            foreach (var span in allRunningSpans)
            {
                if (ret.Count == maxSpansToReturn)
                {
                    break;
                }

                if (span.Name.Equals(filter.SpanName))
                {
                    ret.Add(span.ToSpanData());
                }
            }

            return ret;
        }

        public override void OnEnd(ISpan span)
        {
            SpanBase spanBase = span as SpanBase;
            if (spanBase != null)
            {
                runningSpans.RemoveElement(spanBase);
            }
        }

        public override void OnStart(ISpan span)
        {
            SpanBase spanBase = span as SpanBase;
            if (spanBase != null)
            {
                runningSpans.AddElement(spanBase);
            }
        }
    }
}
