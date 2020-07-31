// <copyright file="Span.cs" company="OpenCensus Authors">
// Copyright 2018, OpenCensus Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenCensus.Trace
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using OpenCensus.Common;
    using OpenCensus.Internal;
    using OpenCensus.Trace.Config;
    using OpenCensus.Trace.Export;
    using OpenCensus.Trace.Internal;
    using OpenCensus.Utils;

    public sealed class Span : SpanBase
    {
        private readonly ISpanId parentSpanId;
        private readonly bool? hasRemoteParent;
        private readonly ITraceParams traceParams;
        private readonly IStartEndHandler startEndHandler;
        private readonly IClock clock;
        private readonly long startNanoTime;
        private readonly object @lock = new object();
        private AttributesWithCapacity attributes;
        private TraceEvents<EventWithNanoTime<IAnnotation>> annotations;
        private TraceEvents<EventWithNanoTime<IMessageEvent>> messageEvents;
        private TraceEvents<ILink> links;
        private Status status;
        private long endNanoTime;
        private bool hasBeenEnded;
        private bool sampleToLocalSpanStore;

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
            Name = name;
            this.traceParams = traceParams ?? throw new ArgumentNullException(nameof(traceParams));
            this.startEndHandler = startEndHandler;
            this.clock = clock;
            hasBeenEnded = false;
            sampleToLocalSpanStore = false;
            if (options.HasFlag(SpanOptions.RecordEvents))
            {
                TimestampConverter = timestampConverter ?? OpenCensus.Internal.TimestampConverter.Now(clock);
                startNanoTime = clock.NowNanos;
            }
            else
            {
                startNanoTime = 0;
                TimestampConverter = timestampConverter;
            }
        }

        /// <inheritdoc/>
        public override string Name { get; set; }

        public override Status Status
        {
            get
            {
                lock (@lock)
                {
                    return StatusWithDefault;
                }
            }

            set
            {
                if (!Options.HasFlag(SpanOptions.RecordEvents))
                {
                    return;
                }

                lock (@lock)
                {
                    if (hasBeenEnded)
                    {
                        // logger.log(Level.FINE, "Calling setStatus() on an ended Span.");
                        return;
                    }

                    status = value;
                }
            }
        }

        public override SpanKind? Kind
        {
            get;

            set; // TODO: do we need to notify when attempt to set on already closed Span?
        }

        public override long EndNanoTime
        {
            get
            {
                lock (@lock)
                {
                    return hasBeenEnded ? endNanoTime : clock.NowNanos;
                }
            }
        }

        public override long LatencyNs
        {
            get
            {
                lock (@lock)
                {
                    return hasBeenEnded ? endNanoTime - startNanoTime : clock.NowNanos - startNanoTime;
                }
            }
        }

        public override bool IsSampleToLocalSpanStore
        {
            get
            {
                lock (@lock)
                {
                    if (!hasBeenEnded)
                    {
                        throw new InvalidOperationException("Running span does not have the SampleToLocalSpanStore set.");
                    }

                    return sampleToLocalSpanStore;
                }
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

        internal ITimestampConverter TimestampConverter { get; private set; }

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
                return status ?? Trace.Status.Ok;
            }
        }

        public override void PutAttribute(string key, IAttributeValue value)
        {
            if (!Options.HasFlag(SpanOptions.RecordEvents))
            {
                return;
            }

            lock (@lock)
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
            if (!Options.HasFlag(SpanOptions.RecordEvents))
            {
                return;
            }

            lock (@lock)
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
            if (!Options.HasFlag(SpanOptions.RecordEvents))
            {
                return;
            }

            lock (@lock)
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
            if (!Options.HasFlag(SpanOptions.RecordEvents))
            {
                return;
            }

            lock (@lock)
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
            if (!Options.HasFlag(SpanOptions.RecordEvents))
            {
                return;
            }

            lock (@lock)
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
            if (!Options.HasFlag(SpanOptions.RecordEvents))
            {
                return;
            }

            lock (@lock)
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
            if (!Options.HasFlag(SpanOptions.RecordEvents))
            {
                return;
            }

            lock (@lock)
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

        // public virtual void AddMessageEvent(MessageEventBase messageEvent)
        // {
        // Default implementation by invoking addNetworkEvent() so that any existing derived classes,
        // including implementation and the mocked ones, do not need to override this method explicitly.
        // addNetworkEvent(BaseMessageEventUtil.asNetworkEvent(messageEvent));
        // }

        public override ISpanData ToSpanData()
        {
            if (!Options.HasFlag(SpanOptions.RecordEvents))
            {
                throw new InvalidOperationException("Getting SpanData for a Span without RECORD_EVENTS option.");
            }

            var attributesSpanData = attributes == null ? Attributes.Create(new Dictionary<string, IAttributeValue>(), 0)
                        : Attributes.Create(attributes, attributes.NumberOfDroppedAttributes);

            var annotationsSpanData = CreateTimedEvents(InitializedAnnotations, TimestampConverter);
            var messageEventsSpanData = CreateTimedEvents(InitializedMessageEvents, TimestampConverter);
            var linksSpanData = links == null ? LinkList.Create(new List<ILink>(), 0) : LinkList.Create(links.Events.ToList(), links.NumberOfDroppedEvents);

            return SpanData.Create(
                Context,
                parentSpanId,
                hasRemoteParent,
                Name,
                TimestampConverter.ConvertNanoTime(startNanoTime),
                attributesSpanData,
                annotationsSpanData,
                messageEventsSpanData,
                linksSpanData,
                null, // Not supported yet.
                hasBeenEnded ? StatusWithDefault : null,
                Kind.HasValue ? Kind.Value : SpanKind.Client,
                hasBeenEnded ? TimestampConverter.ConvertNanoTime(endNanoTime) : null);
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
            if (span.Options.HasFlag(SpanOptions.RecordEvents))
            {
                startEndHandler.OnStart(span);
            }

            return span;
        }

        private static ITimedEvents<T> CreateTimedEvents<T>(TraceEvents<EventWithNanoTime<T>> events, ITimestampConverter timestampConverter)
        {
            if (events == null)
            {
                IEnumerable<ITimedEvent<T>> empty = Array.Empty<ITimedEvent<T>>();
                return TimedEvents<T>.Create(empty, 0);
            }

            var eventsList = new List<ITimedEvent<T>>(events.Events.Count);
            foreach (var networkEvent in events.Events)
            {
                eventsList.Add(networkEvent.ToSpanDataTimedEvent(timestampConverter));
            }

            return TimedEvents<T>.Create(eventsList, events.NumberOfDroppedEvents);
        }
    }
}
