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

using Steeltoe.Management.Census.Internal;
using Steeltoe.Management.Census.Trace.Export;
using System;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class StartEndHandler : IStartEndHandler
    {
        private readonly ISpanExporter _spanExporter;
        private readonly IRunningSpanStore _runningSpanStore;
        private readonly ISampledSpanStore _sampledSpanStore;
        private readonly IEventQueue _eventQueue;

        // true if any of (runningSpanStore OR sampledSpanStore) are different than null, which
        // means the spans with RECORD_EVENTS should be enqueued in the queue.
        private readonly bool _enqueueEventForNonSampledSpans;

        public StartEndHandler(ISpanExporter spanExporter, IRunningSpanStore runningSpanStore, ISampledSpanStore sampledSpanStore, IEventQueue eventQueue)
        {
            this._spanExporter = spanExporter;
            this._runningSpanStore = runningSpanStore;
            this._sampledSpanStore = sampledSpanStore;
            this._enqueueEventForNonSampledSpans = runningSpanStore != null || sampledSpanStore != null;
            this._eventQueue = eventQueue;
        }

        public void OnEnd(SpanBase span)
        {
            if ((span.Options.HasFlag(SpanOptions.RECORD_EVENTS) && _enqueueEventForNonSampledSpans)
                || span.Context.TraceOptions.IsSampled)
            {
                _eventQueue.Enqueue(new SpanEndEvent(span, _spanExporter, _runningSpanStore, _sampledSpanStore));
            }
        }

        public void OnStart(SpanBase span)
        {
            if (span.Options.HasFlag(SpanOptions.RECORD_EVENTS) && _enqueueEventForNonSampledSpans)
            {
                _eventQueue.Enqueue(new SpanStartEvent(span, _runningSpanStore));
            }
        }

        private sealed class SpanStartEvent : IEventQueueEntry
        {
            private readonly SpanBase span;
            private readonly IRunningSpanStore activeSpansExporter;

            public SpanStartEvent(SpanBase span, IRunningSpanStore activeSpansExporter)
            {
                this.span = span;
                this.activeSpansExporter = activeSpansExporter;
            }

            public void Process()
            {
                if (activeSpansExporter != null)
                {
                    activeSpansExporter.OnStart(span);
                }
            }
        }

        private sealed class SpanEndEvent : IEventQueueEntry
        {
            private readonly SpanBase span;
            private readonly IRunningSpanStore runningSpanStore;
            private readonly ISpanExporter spanExporter;
            private readonly ISampledSpanStore sampledSpanStore;

            public SpanEndEvent(
                    SpanBase span,
                    ISpanExporter spanExporter,
                    IRunningSpanStore runningSpanStore,
                    ISampledSpanStore sampledSpanStore)
            {
                this.span = span;
                this.runningSpanStore = runningSpanStore;
                this.spanExporter = spanExporter;
                this.sampledSpanStore = sampledSpanStore;
            }

            public void Process()
            {
                if (span.Context.TraceOptions.IsSampled)
                {
                    spanExporter.AddSpan(span);
                }

                if (runningSpanStore != null)
                {
                    runningSpanStore.OnEnd(span);
                }

                if (sampledSpanStore != null)
                {
                    sampledSpanStore.ConsiderForSampling(span);
                }
            }
        }
    }
}
