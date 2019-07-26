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
using Steeltoe.Management.Census.Trace.Internal;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Export.Test
{
    [Obsolete]
    public class SpanDataTest
    {
        private static readonly ITimestamp StartTimestamp = Timestamp.Create(123, 456);
        private static readonly ITimestamp EventTimestamp1 = Timestamp.Create(123, 457);
        private static readonly ITimestamp EventTimestamp2 = Timestamp.Create(123, 458);
        private static readonly ITimestamp EventTimestamp3 = Timestamp.Create(123, 459);
        private static readonly ITimestamp EndTimestamp = Timestamp.Create(123, 460);
        private static readonly string SPAN_NAME = "MySpanName";
        private static readonly string ANNOTATION_TEXT = "MyAnnotationText";
        private static readonly IAnnotation Annotation = Trace.Annotation.FromDescription(ANNOTATION_TEXT);

        // private static readonly NetworkEvent recvNetworkEvent =
        //    NetworkEvent.Builder(NetworkEvent.Type.RECV, 1).build();
        //      private static readonly NetworkEvent sentNetworkEvent =
        //    NetworkEvent.Builder(NetworkEvent.Type.SENT, 1).build();
        private static readonly IMessageEvent RecvMessageEvent = MessageEvent.Builder(MessageEventType.RECEIVED, 1).Build();
        private static readonly IMessageEvent SentMessageEvent = MessageEvent.Builder(MessageEventType.SENT, 1).Build();
        private static readonly Status Status = Status.DEADLINE_EXCEEDED.WithDescription("TooSlow");
        private static readonly int CHILD_SPAN_COUNT = 13;
        private readonly IRandomGenerator random = new RandomGenerator(1234);
        private readonly ISpanContext spanContext;
        private readonly ISpanId parentSpanId;
        private readonly IDictionary<string, IAttributeValue> attributesMap = new Dictionary<string, IAttributeValue>();
        private readonly IList<ITimedEvent<IAnnotation>> annotationsList = new List<ITimedEvent<IAnnotation>>();

        // private readonly List<TimedEvent<NetworkEvent>> networkEventsList =
        //    new List<SpanData.TimedEvent<NetworkEvent>>();
        private readonly IList<ITimedEvent<IMessageEvent>> messageEventsList = new List<ITimedEvent<IMessageEvent>>();
        private readonly IList<ILink> linksList = new List<ILink>();

        private readonly IAttributes attributes;
        private readonly ITimedEvents<IAnnotation> annotations;

        // private TimedEvents<NetworkEvent> networkEvents;
        private readonly ITimedEvents<IMessageEvent> messageEvents;
        private readonly LinkList links;

        public SpanDataTest()
        {
            spanContext = SpanContext.Create(TraceId.GenerateRandomId(random), SpanId.GenerateRandomId(random), TraceOptions.DEFAULT);
            parentSpanId = SpanId.GenerateRandomId(random);

            attributesMap.Add("MyAttributeKey1", AttributeValue.LongAttributeValue(10));
            attributesMap.Add("MyAttributeKey2", AttributeValue.BooleanAttributeValue(true));
            attributes = Attributes.Create(attributesMap, 1);

            annotationsList.Add(TimedEvent<IAnnotation>.Create(EventTimestamp1, Annotation));
            annotationsList.Add(TimedEvent<IAnnotation>.Create(EventTimestamp3, Annotation));
            annotations = TimedEvents<IAnnotation>.Create(annotationsList, 2);

            // networkEventsList.add(SpanData.TimedEvent.Create(eventTimestamp1, recvNetworkEvent));
            // networkEventsList.add(SpanData.TimedEvent.Create(eventTimestamp2, sentNetworkEvent));
            //// networkEvents = TimedEvents.Create(networkEventsList, 3);

            messageEventsList.Add(TimedEvent<IMessageEvent>.Create(EventTimestamp1, RecvMessageEvent));
            messageEventsList.Add(TimedEvent<IMessageEvent>.Create(EventTimestamp2, SentMessageEvent));
            messageEvents = TimedEvents<IMessageEvent>.Create(messageEventsList, 3);

            linksList.Add(Link.FromSpanContext(spanContext, LinkType.CHILD_LINKED_SPAN));
            links = LinkList.Create(linksList, 0);
        }

        [Fact]
        public void SpanData_AllValues()
        {
            ISpanData spanData =
                SpanData.Create(
                    spanContext,
                    parentSpanId,
                    true,
                    SPAN_NAME,
                    StartTimestamp,
                    attributes,
                    annotations,
                    messageEvents,
                    links,
                    CHILD_SPAN_COUNT,
                    Status,
                    EndTimestamp);
            Assert.Equal(spanContext, spanData.Context);
            Assert.Equal(parentSpanId, spanData.ParentSpanId);
            Assert.True(spanData.HasRemoteParent);
            Assert.Equal(SPAN_NAME, spanData.Name);
            Assert.Equal(StartTimestamp, spanData.StartTimestamp);
            Assert.Equal(attributes, spanData.Attributes);
            Assert.Equal(annotations, spanData.Annotations);
            Assert.Equal(messageEvents, spanData.MessageEvents);
            Assert.Equal(links, spanData.Links);
            Assert.Equal(CHILD_SPAN_COUNT, spanData.ChildSpanCount);
            Assert.Equal(Status, spanData.Status);
            Assert.Equal(EndTimestamp, spanData.EndTimestamp);
        }

        //// [Fact]
        //// public void SpanData_Create_Compatibility()
        //// {
        ////    SpanData spanData =
        ////        SpanData.Create(
        ////            spanContext,
        ////            parentSpanId,
        ////            true,
        ////            SPAN_NAME,
        ////            startTimestamp,
        ////            attributes,
        ////            annotations,
        ////            networkEvents,
        ////            links,
        ////            CHILD_SPAN_COUNT,
        ////            status,
        ////            endTimestamp);
        ////    Assert.Equal(spanData.getContext()).isEqualTo(spanContext);
        ////    Assert.Equal(spanData.getParentSpanId()).isEqualTo(parentSpanId);
        ////    Assert.Equal(spanData.getHasRemoteParent()).isTrue();
        ////    Assert.Equal(spanData.getName()).isEqualTo(SPAN_NAME);
        ////    Assert.Equal(spanData.getStartTimestamp()).isEqualTo(startTimestamp);
        ////    Assert.Equal(spanData.getAttributes()).isEqualTo(attributes);
        ////    Assert.Equal(spanData.getAnnotations()).isEqualTo(annotations);
        ////    Assert.Equal(spanData.getNetworkEvents()).isEqualTo(networkEvents);
        ////    Assert.Equal(spanData.getMessageEvents()).isEqualTo(messageEvents);
        ////    Assert.Equal(spanData.getLinks()).isEqualTo(links);
        ////    Assert.Equal(spanData.getChildSpanCount()).isEqualTo(CHILD_SPAN_COUNT);
        ////    Assert.Equal(spanData.getStatus()).isEqualTo(status);
        ////    Assert.Equal(spanData.getEndTimestamp()).isEqualTo(endTimestamp);
        //// }

        [Fact]
        public void SpanData_RootActiveSpan()
        {
            ISpanData spanData =
                SpanData.Create(
                    spanContext,
                    null,
                    null,
                    SPAN_NAME,
                    StartTimestamp,
                    attributes,
                    annotations,
                    messageEvents,
                    links,
                    null,
                    null,
                    null);
            Assert.Equal(spanContext, spanData.Context);
            Assert.Null(spanData.ParentSpanId);
            Assert.Null(spanData.HasRemoteParent);
            Assert.Equal(SPAN_NAME, spanData.Name);
            Assert.Equal(StartTimestamp, spanData.StartTimestamp);
            Assert.Equal(attributes, spanData.Attributes);
            Assert.Equal(annotations, spanData.Annotations);
            Assert.Equal(messageEvents, spanData.MessageEvents);
            Assert.Equal(links, spanData.Links);
            Assert.Null(spanData.ChildSpanCount);
            Assert.Null(spanData.Status);
            Assert.Null(spanData.EndTimestamp);
        }

        [Fact]
        public void SpanData_AllDataEmpty()
        {
            ISpanData spanData =
                SpanData.Create(
                    spanContext,
                    parentSpanId,
                    false,
                    SPAN_NAME,
                    StartTimestamp,
                    Attributes.Create(new Dictionary<string, IAttributeValue>(), 0),
                    TimedEvents<IAnnotation>.Create(new List<ITimedEvent<IAnnotation>>(), 0),
                    TimedEvents<IMessageEvent>.Create(new List<ITimedEvent<IMessageEvent>>(), 0),
                    LinkList.Create(new List<ILink>(), 0),
                    0,
                    Status,
                    EndTimestamp);

            Assert.Equal(spanContext, spanData.Context);
            Assert.Equal(parentSpanId, spanData.ParentSpanId);
            Assert.False(spanData.HasRemoteParent);
            Assert.Equal(SPAN_NAME, spanData.Name);
            Assert.Equal(StartTimestamp, spanData.StartTimestamp);
            Assert.Empty(spanData.Attributes.AttributeMap);
            Assert.Empty(spanData.Annotations.Events);
            Assert.Empty(spanData.MessageEvents.Events);
            Assert.Empty(spanData.Links.Links);
            Assert.Equal(0, spanData.ChildSpanCount);
            Assert.Equal(Status, spanData.Status);
            Assert.Equal(EndTimestamp, spanData.EndTimestamp);
        }

        [Fact]
        public void SpanDataEquals()
        {
            ISpanData allSpanData1 =
                SpanData.Create(
                    spanContext,
                    parentSpanId,
                    false,
                    SPAN_NAME,
                    StartTimestamp,
                    attributes,
                    annotations,
                    messageEvents,
                    links,
                    CHILD_SPAN_COUNT,
                    Status,
                    EndTimestamp);
            ISpanData allSpanData2 =
                SpanData.Create(
                    spanContext,
                    parentSpanId,
                    false,
                    SPAN_NAME,
                    StartTimestamp,
                    attributes,
                    annotations,
                    messageEvents,
                    links,
                    CHILD_SPAN_COUNT,
                    Status,
                    EndTimestamp);
            ISpanData emptySpanData =
                SpanData.Create(
                    spanContext,
                    parentSpanId,
                    false,
                    SPAN_NAME,
                    StartTimestamp,
                    Attributes.Create(new Dictionary<string, IAttributeValue>(), 0),
                    TimedEvents<IAnnotation>.Create(new List<ITimedEvent<IAnnotation>>(), 0),
                    TimedEvents<IMessageEvent>.Create(new List<ITimedEvent<IMessageEvent>>(), 0),
                    LinkList.Create(new List<ILink>(), 0),
                    0,
                    Status,
                    EndTimestamp);

            Assert.Equal(allSpanData1, allSpanData2);
            Assert.NotEqual(emptySpanData, allSpanData1);
            Assert.NotEqual(emptySpanData, allSpanData2);
        }

        [Fact]
        public void SpanData_ToString()
        {
            string spanDataString =
                SpanData.Create(
                        spanContext,
                        parentSpanId,
                        false,
                        SPAN_NAME,
                        StartTimestamp,
                        attributes,
                        annotations,
                        messageEvents,
                        links,
                        CHILD_SPAN_COUNT,
                        Status,
                        EndTimestamp)
                    .ToString();
            Assert.Contains(spanContext.ToString(), spanDataString);
            Assert.Contains(parentSpanId.ToString(), spanDataString);
            Assert.Contains(SPAN_NAME, spanDataString);
            Assert.Contains(StartTimestamp.ToString(), spanDataString);
            Assert.Contains(attributes.ToString(), spanDataString);
            Assert.Contains(annotations.ToString(), spanDataString);
            Assert.Contains(messageEvents.ToString(), spanDataString);
            Assert.Contains(links.ToString(), spanDataString);
            Assert.Contains(Status.ToString(), spanDataString);
            Assert.Contains(EndTimestamp.ToString(), spanDataString);
        }
    }
}
