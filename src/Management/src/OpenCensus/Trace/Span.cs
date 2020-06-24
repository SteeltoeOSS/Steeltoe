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
            this.Name = name;
            this.traceParams = traceParams ?? throw new ArgumentNullException(nameof(traceParams));
            this.startEndHandler = startEndHandler;
            this.clock = clock;
            this.hasBeenEnded = false;
            this.sampleToLocalSpanStore = false;
            if (options.HasFlag(SpanOptions.RecordEvents))
            {
                this.TimestampConverter = timestampConverter ?? OpenCensus.Internal.TimestampConverter.Now(clock);
                this.startNanoTime = clock.NowNanos;
            }
            else
            {
                this.startNanoTime = 0;
                this.TimestampConverter = timestampConverter;
            }
        }

        /// <inheritdoc/>
        public override string Name { get; set; }

        public override Status Status
        {
            get
            {
                lock (this.@lock)
                {
                    return this.StatusWithDefault;
                }
            }

            set
            {
                if (!this.Options.HasFlag(SpanOptions.RecordEvents))
                {
                    return;
                }

                lock (this.@lock)
                {
                    if (this.hasBeenEnded)
                    {
                        // logger.log(Level.FINE, "Calling setStatus() on an ended Span.");
                        return;
                    }

                    this.status = value;
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
                lock (this.@lock)
                {
                    return this.hasBeenEnded ? this.endNanoTime : this.clock.NowNanos;
                }
            }
        }

        public override long LatencyNs
        {
            get
            {
                lock (this.@lock)
                {
                    return this.hasBeenEnded ? this.endNanoTime - this.startNanoTime : this.clock.NowNanos - this.startNanoTime;
                }
            }
        }

        public override bool IsSampleToLocalSpanStore
        {
            get
            {
                lock (this.@lock)
                {
                    if (!this.hasBeenEnded)
                    {
                        throw new InvalidOperationException("Running span does not have the SampleToLocalSpanStore set.");
                    }

                    return this.sampleToLocalSpanStore;
                }
            }
        }

        public override ISpanId ParentSpanId
        {
            get
            {
                return this.parentSpanId;
            }
        }

        public override bool HasEnded
        {
            get
            {
                return this.hasBeenEnded;
            }
        }

        internal ITimestampConverter TimestampConverter { get; private set; }

        private AttributesWithCapacity InitializedAttributes
        {
            get
            {
                if (this.attributes == null)
                {
                    this.attributes = new AttributesWithCapacity(this.traceParams.MaxNumberOfAttributes);
                }

                return this.attributes;
            }
        }

        private TraceEvents<EventWithNanoTime<IAnnotation>> InitializedAnnotations
        {
            get
            {
                if (this.annotations == null)
                {
                    this.annotations =
                        new TraceEvents<EventWithNanoTime<IAnnotation>>(this.traceParams.MaxNumberOfAnnotations);
                }

                return this.annotations;
            }
        }

        private TraceEvents<EventWithNanoTime<IMessageEvent>> InitializedMessageEvents
        {
            get
            {
                if (this.messageEvents == null)
                {
                    this.messageEvents =
                        new TraceEvents<EventWithNanoTime<IMessageEvent>>(this.traceParams.MaxNumberOfMessageEvents);
                }

                return this.messageEvents;
            }
        }

        private TraceEvents<ILink> InitializedLinks
        {
            get
            {
                if (this.links == null)
                {
                    this.links = new TraceEvents<ILink>(this.traceParams.MaxNumberOfLinks);
                }

                return this.links;
            }
        }

        private Status StatusWithDefault
        {
            get
            {
                return this.status ?? Trace.Status.Ok;
            }
        }

        public override void PutAttribute(string key, IAttributeValue value)
        {
            if (!this.Options.HasFlag(SpanOptions.RecordEvents))
            {
                return;
            }

            lock (this.@lock)
            {
                if (this.hasBeenEnded)
                {
                    // logger.log(Level.FINE, "Calling putAttributes() on an ended Span.");
                    return;
                }

                this.InitializedAttributes.PutAttribute(key, value);
            }
        }

        public override void PutAttributes(IDictionary<string, IAttributeValue> attributes)
        {
            if (!this.Options.HasFlag(SpanOptions.RecordEvents))
            {
                return;
            }

            lock (this.@lock)
            {
                if (this.hasBeenEnded)
                {
                    // logger.log(Level.FINE, "Calling putAttributes() on an ended Span.");
                    return;
                }

                this.InitializedAttributes.PutAttributes(attributes);
            }
        }

        public override void AddAnnotation(string description, IDictionary<string, IAttributeValue> attributes)
        {
            if (!this.Options.HasFlag(SpanOptions.RecordEvents))
            {
                return;
            }

            lock (this.@lock)
            {
                if (this.hasBeenEnded)
                {
                    // logger.log(Level.FINE, "Calling addAnnotation() on an ended Span.");
                    return;
                }

                this.InitializedAnnotations.AddEvent(new EventWithNanoTime<IAnnotation>(this.clock.NowNanos, Annotation.FromDescriptionAndAttributes(description, attributes)));
            }
        }

        public override void AddAnnotation(IAnnotation annotation)
        {
            if (!this.Options.HasFlag(SpanOptions.RecordEvents))
            {
                return;
            }

            lock (this.@lock)
            {
                if (this.hasBeenEnded)
                {
                    // logger.log(Level.FINE, "Calling addAnnotation() on an ended Span.");
                    return;
                }

                if (annotation == null)
                {
                    throw new ArgumentNullException(nameof(annotation));
                }

                this.InitializedAnnotations.AddEvent(new EventWithNanoTime<IAnnotation>(this.clock.NowNanos, annotation));
            }
        }

        public override void AddLink(ILink link)
        {
            if (!this.Options.HasFlag(SpanOptions.RecordEvents))
            {
                return;
            }

            lock (this.@lock)
            {
                if (this.hasBeenEnded)
                {
                    // logger.log(Level.FINE, "Calling addLink() on an ended Span.");
                    return;
                }

                if (link == null)
                {
                    throw new ArgumentNullException(nameof(link));
                }

                this.InitializedLinks.AddEvent(link);
            }
        }

        public override void AddMessageEvent(IMessageEvent messageEvent)
        {
            if (!this.Options.HasFlag(SpanOptions.RecordEvents))
            {
                return;
            }

            lock (this.@lock)
            {
                if (this.hasBeenEnded)
                {
                    // logger.log(Level.FINE, "Calling addNetworkEvent() on an ended Span.");
                    return;
                }

                if (messageEvent == null)
                {
                    throw new ArgumentNullException(nameof(messageEvent));
                }

                this.InitializedMessageEvents.AddEvent(new EventWithNanoTime<IMessageEvent>(this.clock.NowNanos, messageEvent));
            }
        }

        public override void End(EndSpanOptions options)
        {
            if (!this.Options.HasFlag(SpanOptions.RecordEvents))
            {
                return;
            }

            lock (this.@lock)
            {
                if (this.hasBeenEnded)
                {
                    // logger.log(Level.FINE, "Calling end() on an ended Span.");
                    return;
                }

                if (options.Status != null)
                {
                    this.status = options.Status;
                }

                this.sampleToLocalSpanStore = options.SampleToLocalSpanStore;
                this.endNanoTime = this.clock.NowNanos;
                this.hasBeenEnded = true;
            }

            this.startEndHandler.OnEnd(this);
        }

        // public virtual void AddMessageEvent(MessageEventBase messageEvent)
        // {
        // Default implementation by invoking addNetworkEvent() so that any existing derived classes,
        // including implementation and the mocked ones, do not need to override this method explicitly.
        // addNetworkEvent(BaseMessageEventUtil.asNetworkEvent(messageEvent));
        // }

        public override ISpanData ToSpanData()
        {
            if (!this.Options.HasFlag(SpanOptions.RecordEvents))
            {
                throw new InvalidOperationException("Getting SpanData for a Span without RECORD_EVENTS option.");
            }

            Attributes attributesSpanData = this.attributes == null ? Attributes.Create(new Dictionary<string, IAttributeValue>(), 0)
                        : Attributes.Create(this.attributes, this.attributes.NumberOfDroppedAttributes);

            ITimedEvents<IAnnotation> annotationsSpanData = CreateTimedEvents(this.InitializedAnnotations, this.TimestampConverter);
            ITimedEvents<IMessageEvent> messageEventsSpanData = CreateTimedEvents(this.InitializedMessageEvents, this.TimestampConverter);
            LinkList linksSpanData = this.links == null ? LinkList.Create(new List<ILink>(), 0) : LinkList.Create(this.links.Events.ToList(), this.links.NumberOfDroppedEvents);

            return SpanData.Create(
                this.Context,
                this.parentSpanId,
                this.hasRemoteParent,
                this.Name,
                this.TimestampConverter.ConvertNanoTime(this.startNanoTime),
                attributesSpanData,
                annotationsSpanData,
                messageEventsSpanData,
                linksSpanData,
                null, // Not supported yet.
                this.hasBeenEnded ? this.StatusWithDefault : null,
                this.Kind.HasValue ? this.Kind.Value : SpanKind.Client,
                this.hasBeenEnded ? this.TimestampConverter.ConvertNanoTime(this.endNanoTime) : null);
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
                IEnumerable<ITimedEvent<T>> empty = new ITimedEvent<T>[0];
                return TimedEvents<T>.Create(empty, 0);
            }

            var eventsList = new List<ITimedEvent<T>>(events.Events.Count);
            foreach (EventWithNanoTime<T> networkEvent in events.Events)
            {
                eventsList.Add(networkEvent.ToSpanDataTimedEvent(timestampConverter));
            }

            return TimedEvents<T>.Create(eventsList, events.NumberOfDroppedEvents);
        }
    }
}
