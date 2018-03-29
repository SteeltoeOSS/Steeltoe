using Steeltoe.Management.Census.Internal;
using Steeltoe.Management.Census.Trace.Export;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
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
