// <copyright file="SpanData.cs" company="OpenCensus Authors">
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

namespace OpenCensus.Trace.Export
{
    using System;
    using System.Collections.Generic;
    using OpenCensus.Common;

    public sealed class SpanData : ISpanData
    {
        internal SpanData(
            ISpanContext context,
            ISpanId parentSpanId,
            bool? hasRemoteParent,
            string name,
            ITimestamp startTimestamp,
            IAttributes attributes,
            ITimedEvents<IAnnotation> annotations,
            ITimedEvents<IMessageEvent> messageEvents,
            ILinks links,
            int? childSpanCount,
            Status status,
            SpanKind kind,
            ITimestamp endTimestamp)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            ParentSpanId = parentSpanId;
            HasRemoteParent = hasRemoteParent;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            StartTimestamp = startTimestamp ?? throw new ArgumentNullException(nameof(startTimestamp));
            Attributes = attributes ?? Export.Attributes.Create(new Dictionary<string, IAttributeValue>(), 0);
            Annotations = annotations ?? TimedEvents<IAnnotation>.Create(new List<ITimedEvent<IAnnotation>>(), 0);
            MessageEvents = messageEvents ?? TimedEvents<IMessageEvent>.Create(new List<ITimedEvent<IMessageEvent>>(), 0);
            Links = links ?? LinkList.Create(new List<ILink>(), 0);
            ChildSpanCount = childSpanCount;
            Status = status;
            Kind = kind;
            EndTimestamp = endTimestamp;
        }

        public ISpanContext Context { get; }

        public ISpanId ParentSpanId { get; }

        public bool? HasRemoteParent { get; }

        public string Name { get; }

        public ITimestamp Timestamp { get; }

        public IAttributes Attributes { get; }

        public ITimedEvents<IAnnotation> Annotations { get; }

        public ITimedEvents<IMessageEvent> MessageEvents { get; }

        public ILinks Links { get; }

        public int? ChildSpanCount { get; }

        public Status Status { get; }

        public SpanKind Kind { get; }

        public ITimestamp EndTimestamp { get; }

        public ITimestamp StartTimestamp { get; }

        public static ISpanData Create(
                        ISpanContext context,
                        ISpanId parentSpanId,
                        bool? hasRemoteParent,
                        string name,
                        ITimestamp startTimestamp,
                        IAttributes attributes,
                        ITimedEvents<IAnnotation> annotations,
                        ITimedEvents<IMessageEvent> messageOrNetworkEvents,
                        ILinks links,
                        int? childSpanCount,
                        Status status,
                        SpanKind kind,
                        ITimestamp endTimestamp)
        {
            if (messageOrNetworkEvents == null)
            {
                messageOrNetworkEvents = TimedEvents<IMessageEvent>.Create(new List<ITimedEvent<IMessageEvent>>(), 0);
            }

            var messageEventsList = new List<ITimedEvent<IMessageEvent>>();
            foreach (var timedEvent in messageOrNetworkEvents.Events)
            {
                messageEventsList.Add(timedEvent);
            }

            var messageEvents = TimedEvents<IMessageEvent>.Create(messageEventsList, messageOrNetworkEvents.DroppedEventsCount);
            return new SpanData(
                context,
                parentSpanId,
                hasRemoteParent,
                name,
                startTimestamp,
                attributes,
                annotations,
                messageEvents,
                links,
                childSpanCount,
                status,
                kind,
                endTimestamp);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "SpanData{"
                + "context=" + Context + ", "
                + "parentSpanId=" + ParentSpanId + ", "
                + "hasRemoteParent=" + HasRemoteParent + ", "
                + "name=" + Name + ", "
                + "startTimestamp=" + StartTimestamp + ", "
                + "attributes=" + Attributes + ", "
                + "annotations=" + Annotations + ", "
                + "messageEvents=" + MessageEvents + ", "
                + "links=" + Links + ", "
                + "childSpanCount=" + ChildSpanCount + ", "
                + "status=" + Status + ", "
                + "endTimestamp=" + EndTimestamp
                + "}";
        }

        /// <inheritdoc/>
        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is SpanData that)
            {
                return Context.Equals(that.Context)
                     && ((ParentSpanId == null) ? (that.ParentSpanId == null) : ParentSpanId.Equals(that.ParentSpanId))
                     && HasRemoteParent.Equals(that.HasRemoteParent)
                     && Name.Equals(that.Name)
                     && StartTimestamp.Equals(that.StartTimestamp)
                     && Attributes.Equals(that.Attributes)
                     && Annotations.Equals(that.Annotations)
                     && MessageEvents.Equals(that.MessageEvents)
                     && Links.Equals(that.Links)
                     && ((ChildSpanCount == null) ? (that.ChildSpanCount == null) : ChildSpanCount.Equals(that.ChildSpanCount))
                     && ((Status == null) ? (that.Status == null) : Status.Equals(that.Status))
                     && ((EndTimestamp == null) ? (that.EndTimestamp == null) : EndTimestamp.Equals(that.EndTimestamp));
            }

            return false;
        }

    /// <inheritdoc/>
        public override int GetHashCode()
        {
            var h = 1;
            h *= 1000003;
            h ^= Context.GetHashCode();
            h *= 1000003;
            h ^= (ParentSpanId == null) ? 0 : ParentSpanId.GetHashCode();
            h *= 1000003;
            h ^= (HasRemoteParent == null) ? 0 : HasRemoteParent.GetHashCode();
            h *= 1000003;
            h ^= Name.GetHashCode();
            h *= 1000003;
            h ^= StartTimestamp.GetHashCode();
            h *= 1000003;
            h ^= Attributes.GetHashCode();
            h *= 1000003;
            h ^= Annotations.GetHashCode();
            h *= 1000003;
            h ^= MessageEvents.GetHashCode();
            h *= 1000003;
            h ^= Links.GetHashCode();
            h *= 1000003;
            h ^= (ChildSpanCount == null) ? 0 : ChildSpanCount.GetHashCode();
            h *= 1000003;
            h ^= (Status == null) ? 0 : Status.GetHashCode();
            h *= 1000003;
            h ^= (EndTimestamp == null) ? 0 : EndTimestamp.GetHashCode();
            return h;
        }
    }
}
