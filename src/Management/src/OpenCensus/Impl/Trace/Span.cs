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

using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Internal;
using Steeltoe.Management.Census.Trace.Config;
using Steeltoe.Management.Census.Trace.Export;
using Steeltoe.Management.Census.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class Span : SpanBase
    {
        private readonly ISpanId parentSpanId;
        private readonly bool? hasRemoteParent;
        private readonly ITraceParams traceParams;
        private readonly IStartEndHandler startEndHandler;
        private readonly string name;
        private readonly IClock clock;
        private readonly ITimestampConverter timestampConverter;
        private readonly long startNanoTime;
        private AttributesWithCapacity attributes;
        private TraceEvents<EventWithNanoTime<IAnnotation>> annotations;
        private TraceEvents<EventWithNanoTime<IMessageEvent>> messageEvents;
        private TraceEvents<ILink> links;
        private Status status;
        private long endNanoTime;
        private bool hasBeenEnded;
        private bool sampleToLocalSpanStore;
        private object _lock = new object();

        public override string Name
        {
            get
            {
                return name;
            }
        }

        public override Status Status
        {
            get
            {
                lock (_lock)
                {
                    return StatusWithDefault;
                }
            }

            set
            {
                if (!Options.HasFlag(SpanOptions.RECORD_EVENTS))
                {
                    return;
                }

                lock (_lock)
                {
                    if (hasBeenEnded)
                    {
                        // logger.log(Level.FINE, "Calling setStatus() on an ended Span.");
                        return;
                    }

                    this.status = value;
                }
            }
        }

        public override long EndNanoTime
        {
            get
            {
                lock (_lock)
                {
                    return hasBeenEnded ? endNanoTime : clock.NowNanos;
                }
            }
        }

        public override long LatencyNs
        {
            get
            {
                lock (_lock)
                {
                    return hasBeenEnded ? endNanoTime - startNanoTime : clock.NowNanos - startNanoTime;
                }
            }
        }

        public override bool IsSampleToLocalSpanStore
        {
            get
            {
                lock (_lock)
                {
                    if (!hasBeenEnded)
                    {
                        throw new InvalidOperationException("Running span does not have the SampleToLocalSpanStore set.");
                    }

                    return sampleToLocalSpanStore;
                }
            }
        }

        public override void PutAttribute(string key, IAttributeValue value)
        {
            if (!Options.HasFlag(SpanOptions.RECORD_EVENTS))
            {
                return;
            }

            lock (_lock)
            {
                if (hasBeenEnded)
                {
                    // logger.log(Level.FINE, "Calling putAttributes() on an ended Span.");
                    return;
                }

                InitializedAttributes.PutAttribute(key, value);
            }
        }

        public override void PutAttributes(IDictionary<string, IAttributeValue> attributes)
        {
            if (!Options.HasFlag(SpanOptions.RECORD_EVENTS))
            {
                return;
            }

            lock (_lock)
            {
                if (hasBeenEnded)
                {
                    // logger.log(Level.FINE, "Calling putAttributes() on an ended Span.");
                    return;
                }

                InitializedAttributes.PutAttributes(attributes);
            }
        }

        public override void AddAnnotation(string description, IDictionary<string, IAttributeValue> attributes)
        {
            if (!Options.HasFlag(SpanOptions.RECORD_EVENTS))
            {
                return;
            }

            lock (_lock)
            {
                if (hasBeenEnded)
                {
                    // logger.log(Level.FINE, "Calling addAnnotation() on an ended Span.");
                    return;
                }

                InitializedAnnotations.AddEvent(new EventWithNanoTime<IAnnotation>(clock.NowNanos, Annotation.FromDescriptionAndAttributes(description, attributes)));
            }
        }

        public override void AddAnnotation(IAnnotation annotation)
        {
            if (!Options.HasFlag(SpanOptions.RECORD_EVENTS))
            {
                return;
            }

            lock (_lock)
            {
                if (hasBeenEnded)
                {
                    // logger.log(Level.FINE, "Calling addAnnotation() on an ended Span.");
                    return;
                }

                if (annotation == null)
                {
                    throw new ArgumentNullException(nameof(annotation));
                }

                InitializedAnnotations.AddEvent(new EventWithNanoTime<IAnnotation>(clock.NowNanos, annotation));
            }
        }

        public override void AddLink(ILink link)
        {
            if (!Options.HasFlag(SpanOptions.RECORD_EVENTS))
            {
                return;
            }

            lock (_lock)
            {
                if (hasBeenEnded)
                {
                    // logger.log(Level.FINE, "Calling addLink() on an ended Span.");
                    return;
                }

                if (link == null)
                {
                    throw new ArgumentNullException(nameof(link));
                }

                InitializedLinks.AddEvent(link);
            }
        }

        public override void AddMessageEvent(IMessageEvent messageEvent)
        {
            if (!Options.HasFlag(SpanOptions.RECORD_EVENTS))
            {
                return;
            }

            lock (_lock)
            {
                if (hasBeenEnded)
                {
                    // logger.log(Level.FINE, "Calling addNetworkEvent() on an ended Span.");
                    return;
                }

                if (messageEvent == null)
                {
                    throw new ArgumentNullException(nameof(messageEvent));
                }

                InitializedMessageEvents.AddEvent(new EventWithNanoTime<IMessageEvent>(clock.NowNanos, messageEvent));
            }
        }

        public override void End(EndSpanOptions options)
        {
            if (!Options.HasFlag(SpanOptions.RECORD_EVENTS))
            {
                return;
            }

            lock (_lock)
            {
                if (hasBeenEnded)
                {
                    // logger.log(Level.FINE, "Calling end() on an ended Span.");
                    return;
                }

                if (options.Status != null)
                {
                    status = options.Status;
                }

                sampleToLocalSpanStore = options.SampleToLocalSpanStore;
                endNanoTime = clock.NowNanos;
                hasBeenEnded = true;
            }

            startEndHandler.OnEnd(this);
        }

        private AttributesWithCapacity InitializedAttributes
        {
            get
            {
                if (attributes == null)
                {
                    attributes = new AttributesWithCapacity(traceParams.MaxNumberOfAttributes);
                }

                return attributes;
            }
        }

        private TraceEvents<EventWithNanoTime<IAnnotation>> InitializedAnnotations
        {
            get
            {
                if (annotations == null)
                {
                    annotations =
                        new TraceEvents<EventWithNanoTime<IAnnotation>>(traceParams.MaxNumberOfAnnotations);
                }

                return annotations;
            }
        }

        private TraceEvents<EventWithNanoTime<IMessageEvent>> InitializedMessageEvents
        {
            get
            {
                if (messageEvents == null)
                {
                    messageEvents =
                        new TraceEvents<EventWithNanoTime<IMessageEvent>>(traceParams.MaxNumberOfMessageEvents);
                }

                return messageEvents;
            }
        }

        private TraceEvents<ILink> InitializedLinks
        {
            get
            {
                if (links == null)
                {
                    links = new TraceEvents<ILink>(traceParams.MaxNumberOfLinks);
                }

                return links;
            }
        }

        private Status StatusWithDefault
        {
            get
            {
                return status ?? Trace.Status.OK;
            }
        }

        internal ITimestampConverter TimestampConverter
        {
            get
            {
                return timestampConverter;
            }
        }

        public override ISpanId ParentSpanId
        {
            get
            {
                return parentSpanId;
            }
        }

        public override bool HasEnded
        {
            get
            {
                return hasBeenEnded;
            }
        }

        internal override ISpanData ToSpanData()
        {
            if (!Options.HasFlag(SpanOptions.RECORD_EVENTS))
            {
                throw new InvalidOperationException("Getting SpanData for a Span without RECORD_EVENTS option.");
            }

            Attributes attributesSpanData = attributes == null ? Attributes.Create(new Dictionary<string, IAttributeValue>(), 0)
                        : Attributes.Create(attributes, attributes.NumberOfDroppedAttributes);

            ITimedEvents<IAnnotation> annotationsSpanData = CreateTimedEvents(InitializedAnnotations, timestampConverter);
            ITimedEvents<IMessageEvent> messageEventsSpanData = CreateTimedEvents(InitializedMessageEvents, timestampConverter);
            LinkList linksSpanData = links == null ? LinkList.Create(new List<ILink>(), 0) : LinkList.Create(links.Events.ToList(), links.NumberOfDroppedEvents);

            return SpanData.Create(
                Context,
                parentSpanId,
                hasRemoteParent,
                name,
                timestampConverter.ConvertNanoTime(startNanoTime),
                attributesSpanData,
                annotationsSpanData,
                messageEventsSpanData,
                linksSpanData,
                null, // Not supported yet.
                hasBeenEnded ? StatusWithDefault : null,
                hasBeenEnded ? timestampConverter.ConvertNanoTime(endNanoTime) : null);
        }

        internal static ISpan StartSpan(
                ISpanContext context,
                SpanOptions options,
                string name,
                ISpanId parentSpanId,
                bool? hasRemoteParent,
                ITraceParams traceParams,
                IStartEndHandler startEndHandler,
                ITimestampConverter timestampConverter,
                IClock clock)
        {
            var span = new Span(
               context,
               options,
               name,
               parentSpanId,
               hasRemoteParent,
               traceParams,
               startEndHandler,
               timestampConverter,
               clock);

            // Call onStart here instead of calling in the constructor to make sure the span is completely
            // initialized.
            if (span.Options.HasFlag(SpanOptions.RECORD_EVENTS))
            {
                startEndHandler.OnStart(span);
            }

            return span;
        }

        // public virtual void AddMessageEvent(MessageEventBase messageEvent)
        // {
        // Default implementation by invoking addNetworkEvent() so that any existing derived classes,
        // including implementation and the mocked ones, do not need to override this method explicitly.
        // addNetworkEvent(BaseMessageEventUtil.asNetworkEvent(messageEvent));
        // }

        //// public abstract void AddLink(LinkBase link);

        private Span(
                ISpanContext context,
                SpanOptions options,
                string name,
                ISpanId parentSpanId,
                bool? hasRemoteParent,
                ITraceParams traceParams,
                IStartEndHandler startEndHandler,
                ITimestampConverter timestampConverter,
                IClock clock)
            : base(context, options)
        {
            this.parentSpanId = parentSpanId;
            this.hasRemoteParent = hasRemoteParent;
            this.name = name;
            this.traceParams = traceParams;
            this.startEndHandler = startEndHandler;
            this.clock = clock;
            this.hasBeenEnded = false;
            this.sampleToLocalSpanStore = false;
            if (options.HasFlag(SpanOptions.RECORD_EVENTS))
            {
                this.timestampConverter = timestampConverter != null ? timestampConverter : Census.Internal.TimestampConverter.Now(clock);
                startNanoTime = clock.NowNanos;
            }
            else
            {
                this.startNanoTime = 0;
                this.timestampConverter = timestampConverter;
            }
        }

        private static ITimedEvents<T> CreateTimedEvents<T>(TraceEvents<EventWithNanoTime<T>> events, ITimestampConverter timestampConverter)
        {
            if (events == null)
            {
                IList<ITimedEvent<T>> empty = new List<ITimedEvent<T>>();
                return TimedEvents<T>.Create(empty, 0);
            }

            IList<ITimedEvent<T>> eventsList = new List<ITimedEvent<T>>(events.Events.Count);
            foreach (EventWithNanoTime<T> networkEvent in events.Events)
            {
                eventsList.Add(networkEvent.ToSpanDataTimedEvent(timestampConverter));
            }

            return TimedEvents<T>.Create(eventsList, events.NumberOfDroppedEvents);
        }
    }
}
