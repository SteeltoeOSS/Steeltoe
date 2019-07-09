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

using Moq;
using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Internal;
using Steeltoe.Management.Census.Testing.Common;
using Steeltoe.Management.Census.Trace.Config;
using Steeltoe.Management.Census.Trace.Export;
using Steeltoe.Management.Census.Trace.Internal;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    [Obsolete]
    public class SpanTest
    {
        private static readonly string SPAN_NAME = "MySpanName";
        private static readonly string ANNOTATION_DESCRIPTION = "MyAnnotation";
        private readonly RandomGenerator random = new RandomGenerator(1234);
        private readonly ISpanContext spanContext;
        private readonly ISpanId parentSpanId;
        private readonly ITimestamp timestamp = Timestamp.Create(1234, 5678);
        private readonly TestClock testClock;
        private readonly ITimestampConverter timestampConverter;
        private readonly SpanOptions noRecordSpanOptions = SpanOptions.NONE;
        private readonly SpanOptions recordSpanOptions = SpanOptions.RECORD_EVENTS;
        private readonly IDictionary<string, IAttributeValue> attributes = new Dictionary<string, IAttributeValue>();
        private readonly IDictionary<string, IAttributeValue> expectedAttributes;
        private IStartEndHandler startEndHandler = Mock.Of<IStartEndHandler>();

        //// @Rule public readonly ExpectedException exception = ExpectedException.none();

        public SpanTest()
        {
            spanContext = SpanContext.Create(TraceId.GenerateRandomId(random), SpanId.GenerateRandomId(random), TraceOptions.DEFAULT);
            parentSpanId = SpanId.GenerateRandomId(random);
            testClock = TestClock.Create(timestamp);
            timestampConverter = TimestampConverter.Now(testClock);
            attributes.Add(
                "MyStringAttributeKey", AttributeValue.StringAttributeValue("MyStringAttributeValue"));
            attributes.Add("MyLongAttributeKey", AttributeValue.LongAttributeValue(123L));
            attributes.Add("MyBooleanAttributeKey", AttributeValue.BooleanAttributeValue(false));
            expectedAttributes = new Dictionary<string, IAttributeValue>(attributes)
            {
                {
                    "MySingleStringAttributeKey",
                    AttributeValue.StringAttributeValue("MySingleStringAttributeValue")
                }
            };
        }

        [Fact]
        public void ToSpanData_NoRecordEvents()
        {
            ISpan span =
                Span.StartSpan(
                    spanContext,
                    noRecordSpanOptions,
                    SPAN_NAME,
                    parentSpanId,
                    false,
                    TraceParams.DEFAULT,
                    startEndHandler,
                    timestampConverter,
                    testClock);

            // Check that adding trace events after Span#End() does not throw any exception.
            span.PutAttributes(attributes);
            span.AddAnnotation(Annotation.FromDescription(ANNOTATION_DESCRIPTION));
            span.AddAnnotation(ANNOTATION_DESCRIPTION, attributes);
            span.AddMessageEvent(
                MessageEvent.Builder(MessageEventType.RECEIVED, 1).SetUncompressedMessageSize(3).Build());
            span.AddLink(Link.FromSpanContext(spanContext, LinkType.CHILD_LINKED_SPAN));
            span.End();

            // exception.expect(IllegalStateException);
            Assert.Throws<InvalidOperationException>(() => ((Span)span).ToSpanData());
        }

        [Fact]
        public void NoEventsRecordedAfterEnd()
        {
            ISpan span =
                Span.StartSpan(
                    spanContext,
                    recordSpanOptions,
                    SPAN_NAME,
                    parentSpanId,
                    false,
                    TraceParams.DEFAULT,
                    startEndHandler,
                    timestampConverter,
                    testClock);
            span.End();

            // Check that adding trace events after Span#End() does not throw any exception and are not
            // recorded.
            span.PutAttributes(attributes);
            span.PutAttribute(
                "MySingleStringAttributeKey",
                AttributeValue.StringAttributeValue("MySingleStringAttributeValue"));
            span.AddAnnotation(Annotation.FromDescription(ANNOTATION_DESCRIPTION));
            span.AddAnnotation(ANNOTATION_DESCRIPTION, attributes);
            span.AddMessageEvent(
                MessageEvent.Builder(MessageEventType.RECEIVED, 1).SetUncompressedMessageSize(3).Build());
            span.AddLink(Link.FromSpanContext(spanContext, LinkType.CHILD_LINKED_SPAN));
            ISpanData spanData = ((Span)span).ToSpanData();
            Assert.Equal(timestamp, spanData.StartTimestamp);
            Assert.Empty(spanData.Attributes.AttributeMap);
            Assert.Empty(spanData.Annotations.Events);
            Assert.Empty(spanData.MessageEvents.Events);
            Assert.Empty(spanData.Links.Links);
            Assert.Equal(Status.OK, spanData.Status);
            Assert.Equal(timestamp, spanData.EndTimestamp);
        }

        // [Fact]
        // public void DeprecatedAddAttributesStillWorks()
        //  {
        //      ISpan span =
        //          Span.StartSpan(
        //              spanContext,
        //              recordSpanOptions,
        //              SPAN_NAME,
        //              parentSpanId,
        //              false,
        //              TraceParams.DEFAULT,
        //              startEndHandler,
        //              timestampConverter,
        //              testClock);
        //      span.AddAttributes(attributes);
        //      span.End();
        //      SpanData spanData = ((Span)span).ToSpanData();
        //      Assert.Equal(spanData.Attributes.AttributeMap).isEqualTo(attributes);
        ////  }

        [Fact]
        public void ToSpanData_ActiveSpan()
        {
            ISpan span =
                Span.StartSpan(
                    spanContext,
                    recordSpanOptions,
                    SPAN_NAME,
                    parentSpanId,
                    true,
                    TraceParams.DEFAULT,
                    startEndHandler,
                    timestampConverter,
                    testClock);

            span.PutAttribute(
                "MySingleStringAttributeKey",
                AttributeValue.StringAttributeValue("MySingleStringAttributeValue"));
            span.PutAttributes(attributes);
            testClock.AdvanceTime(Duration.Create(0, 100));
            span.AddAnnotation(Annotation.FromDescription(ANNOTATION_DESCRIPTION));
            testClock.AdvanceTime(Duration.Create(0, 100));
            span.AddAnnotation(ANNOTATION_DESCRIPTION, attributes);
            testClock.AdvanceTime(Duration.Create(0, 100));
            IMessageEvent networkEvent =
                MessageEvent.Builder(MessageEventType.RECEIVED, 1).SetUncompressedMessageSize(3).Build();
            span.AddMessageEvent(networkEvent);
            testClock.AdvanceTime(Duration.Create(0, 100));
            ILink link = Link.FromSpanContext(spanContext, LinkType.CHILD_LINKED_SPAN);
            span.AddLink(link);
            ISpanData spanData = ((Span)span).ToSpanData();
            Assert.Equal(spanContext, spanData.Context);
            Assert.Equal(SPAN_NAME, spanData.Name);
            Assert.Equal(parentSpanId, spanData.ParentSpanId);
            Assert.True(spanData.HasRemoteParent);
            Assert.Equal(0, spanData.Attributes.DroppedAttributesCount);
            Assert.Equal(expectedAttributes, spanData.Attributes.AttributeMap);
            Assert.Equal(0, spanData.Annotations.DroppedEventsCount);
            Assert.Equal(2, spanData.Annotations.Events.Count);
            Assert.Equal(timestamp.AddNanos(100), spanData.Annotations.Events[0].Timestamp);
            Assert.Equal(Annotation.FromDescription(ANNOTATION_DESCRIPTION), spanData.Annotations.Events[0].Event);
            Assert.Equal(timestamp.AddNanos(200), spanData.Annotations.Events[1].Timestamp);
            Assert.Equal(Annotation.FromDescriptionAndAttributes(ANNOTATION_DESCRIPTION, attributes), spanData.Annotations.Events[1].Event);
            Assert.Equal(0, spanData.MessageEvents.DroppedEventsCount);
            Assert.Equal(1, spanData.MessageEvents.Events.Count);
            Assert.Equal(timestamp.AddNanos(300), spanData.MessageEvents.Events[0].Timestamp);
            Assert.Equal(networkEvent, spanData.MessageEvents.Events[0].Event);
            Assert.Equal(0, spanData.Links.DroppedLinksCount);
            Assert.Equal(1, spanData.Links.Links.Count);
            Assert.Equal(link, spanData.Links.Links[0]);
            Assert.Equal(timestamp, spanData.StartTimestamp);
            Assert.Null(spanData.Status);
            Assert.Null(spanData.EndTimestamp);

            var startEndMock = Mock.Get<IStartEndHandler>(startEndHandler);
            var spanBase = span as SpanBase;
            startEndMock.Verify(s => s.OnStart(spanBase), Times.Once);
        }

        [Fact]
        public void GoSpanData_EndedSpan()
        {
            ISpan span =
                Span.StartSpan(
                    spanContext,
                    recordSpanOptions,
                    SPAN_NAME,
                    parentSpanId,
                    false,
                    TraceParams.DEFAULT,
                    startEndHandler,
                    timestampConverter,
                    testClock);

            span.PutAttribute(
                "MySingleStringAttributeKey",
                AttributeValue.StringAttributeValue("MySingleStringAttributeValue"));
            span.PutAttributes(attributes);
            testClock.AdvanceTime(Duration.Create(0, 100));
            span.AddAnnotation(Annotation.FromDescription(ANNOTATION_DESCRIPTION));
            testClock.AdvanceTime(Duration.Create(0, 100));
            span.AddAnnotation(ANNOTATION_DESCRIPTION, attributes);
            testClock.AdvanceTime(Duration.Create(0, 100));
            IMessageEvent networkEvent =
                MessageEvent.Builder(MessageEventType.RECEIVED, 1).SetUncompressedMessageSize(3).Build();
            span.AddMessageEvent(networkEvent);
            ILink link = Link.FromSpanContext(spanContext, LinkType.CHILD_LINKED_SPAN);
            span.AddLink(link);
            testClock.AdvanceTime(Duration.Create(0, 100));
            span.End(EndSpanOptions.Builder().SetStatus(Status.CANCELLED).Build());

            ISpanData spanData = ((Span)span).ToSpanData();
            Assert.Equal(spanContext, spanData.Context);
            Assert.Equal(SPAN_NAME, spanData.Name);
            Assert.Equal(parentSpanId, spanData.ParentSpanId);
            Assert.False(spanData.HasRemoteParent);
            Assert.Equal(0, spanData.Attributes.DroppedAttributesCount);
            Assert.Equal(expectedAttributes, spanData.Attributes.AttributeMap);
            Assert.Equal(0, spanData.Annotations.DroppedEventsCount);
            Assert.Equal(2, spanData.Annotations.Events.Count);
            Assert.Equal(timestamp.AddNanos(100), spanData.Annotations.Events[0].Timestamp);
            Assert.Equal(Annotation.FromDescription(ANNOTATION_DESCRIPTION), spanData.Annotations.Events[0].Event);
            Assert.Equal(timestamp.AddNanos(200), spanData.Annotations.Events[1].Timestamp);
            Assert.Equal(Annotation.FromDescriptionAndAttributes(ANNOTATION_DESCRIPTION, attributes), spanData.Annotations.Events[1].Event);
            Assert.Equal(0, spanData.MessageEvents.DroppedEventsCount);
            Assert.Equal(1, spanData.MessageEvents.Events.Count);
            Assert.Equal(timestamp.AddNanos(300), spanData.MessageEvents.Events[0].Timestamp);
            Assert.Equal(networkEvent, spanData.MessageEvents.Events[0].Event);
            Assert.Equal(0, spanData.Links.DroppedLinksCount);
            Assert.Equal(1, spanData.Links.Links.Count);
            Assert.Equal(link, spanData.Links.Links[0]);
            Assert.Equal(timestamp, spanData.StartTimestamp);
            Assert.Equal(Status.CANCELLED, spanData.Status);
            Assert.Equal(timestamp.AddNanos(400), spanData.EndTimestamp);

            var startEndMock = Mock.Get<IStartEndHandler>(startEndHandler);
            var spanBase = span as SpanBase;
            startEndMock.Verify(s => s.OnStart(spanBase), Times.Once);
            startEndMock.Verify(s => s.OnEnd(spanBase), Times.Once);
        }

        [Fact]
        public void Status_ViaSetStatus()
        {
            ISpan span =
                Span.StartSpan(
                    spanContext,
                    recordSpanOptions,
                    SPAN_NAME,
                    parentSpanId,
                    false,
                    TraceParams.DEFAULT,
                    startEndHandler,
                    timestampConverter,
                    testClock);
            testClock.AdvanceTime(Duration.Create(0, 100));
            Assert.Equal(Status.OK, span.Status);
            ((Span)span).Status = Status.CANCELLED;
            Assert.Equal(Status.CANCELLED, span.Status);
            span.End();
            Assert.Equal(Status.CANCELLED, span.Status);

            var startEndMock = Mock.Get<IStartEndHandler>(startEndHandler);
            var spanBase = span as SpanBase;
            startEndMock.Verify(s => s.OnStart(spanBase), Times.Once);
        }

        [Fact]
        public void Status_ViaEndSpanOptions()
        {
            ISpan span =
                Span.StartSpan(
                    spanContext,
                    recordSpanOptions,
                    SPAN_NAME,
                    parentSpanId,
                    false,
                    TraceParams.DEFAULT,
                    startEndHandler,
                    timestampConverter,
                    testClock);
            testClock.AdvanceTime(Duration.Create(0, 100));
            Assert.Equal(Status.OK, span.Status);
            ((Span)span).Status = Status.CANCELLED;
            Assert.Equal(Status.CANCELLED, span.Status);
            span.End(EndSpanOptions.Builder().SetStatus(Status.ABORTED).Build());
            Assert.Equal(Status.ABORTED, span.Status);

            var startEndMock = Mock.Get<IStartEndHandler>(startEndHandler);
            var spanBase = span as SpanBase;
            startEndMock.Verify(s => s.OnStart(spanBase), Times.Once);
        }

        [Fact]
        public void DroppingAttributes()
        {
            int maxNumberOfAttributes = 8;
            TraceParams traceParams =
                TraceParams.DEFAULT.ToBuilder().SetMaxNumberOfAttributes(maxNumberOfAttributes).Build();
            ISpan span =
                Span.StartSpan(
                    spanContext,
                    recordSpanOptions,
                    SPAN_NAME,
                    parentSpanId,
                    false,
                    traceParams,
                    startEndHandler,
                    timestampConverter,
                    testClock);
            for (int i = 0; i < 2 * maxNumberOfAttributes; i++)
            {
                IDictionary<string, IAttributeValue> attributes = new Dictionary<string, IAttributeValue>
                {
                    { "MyStringAttributeKey" + i, AttributeValue.LongAttributeValue(i) }
                };
                span.PutAttributes(attributes);
            }

            ISpanData spanData = ((Span)span).ToSpanData();
            Assert.Equal(maxNumberOfAttributes, spanData.Attributes.DroppedAttributesCount);
            Assert.Equal(maxNumberOfAttributes, spanData.Attributes.AttributeMap.Count);
            for (int i = 0; i < maxNumberOfAttributes; i++)
            {
                Assert.Equal(
                    AttributeValue.LongAttributeValue(i + maxNumberOfAttributes),
                    spanData.Attributes.AttributeMap["MyStringAttributeKey" + (i + maxNumberOfAttributes)]);
            }

            span.End();
            spanData = ((Span)span).ToSpanData();
            Assert.Equal(maxNumberOfAttributes, spanData.Attributes.DroppedAttributesCount);
            Assert.Equal(maxNumberOfAttributes, spanData.Attributes.AttributeMap.Count);
            for (int i = 0; i < maxNumberOfAttributes; i++)
            {
                Assert.Equal(
                    AttributeValue.LongAttributeValue(i + maxNumberOfAttributes),
                    spanData.Attributes.AttributeMap["MyStringAttributeKey" + (i + maxNumberOfAttributes)]);
            }
        }

        [Fact]
        public void DroppingAndAddingAttributes()
        {
            int maxNumberOfAttributes = 8;
            TraceParams traceParams =
                TraceParams.DEFAULT.ToBuilder().SetMaxNumberOfAttributes(maxNumberOfAttributes).Build();
            ISpan span =
                Span.StartSpan(
                    spanContext,
                    recordSpanOptions,
                    SPAN_NAME,
                    parentSpanId,
                    false,
                    traceParams,
                    startEndHandler,
                    timestampConverter,
                    testClock);
            for (int i = 0; i < 2 * maxNumberOfAttributes; i++)
            {
                IDictionary<string, IAttributeValue> attributes = new Dictionary<string, IAttributeValue>
                {
                    { "MyStringAttributeKey" + i, AttributeValue.LongAttributeValue(i) }
                };
                span.PutAttributes(attributes);
            }

            ISpanData spanData = ((Span)span).ToSpanData();
            Assert.Equal(maxNumberOfAttributes, spanData.Attributes.DroppedAttributesCount);
            Assert.Equal(maxNumberOfAttributes, spanData.Attributes.AttributeMap.Count);
            for (int i = 0; i < maxNumberOfAttributes; i++)
            {
                Assert.Equal(
                    AttributeValue.LongAttributeValue(i + maxNumberOfAttributes),
                    spanData.Attributes.AttributeMap["MyStringAttributeKey" + (i + maxNumberOfAttributes)]);
            }

            for (int i = 0; i < maxNumberOfAttributes / 2; i++)
            {
                IDictionary<string, IAttributeValue> attributes = new Dictionary<string, IAttributeValue>
                {
                    { "MyStringAttributeKey" + i, AttributeValue.LongAttributeValue(i) }
                };
                span.PutAttributes(attributes);
            }

            spanData = ((Span)span).ToSpanData();
            Assert.Equal(maxNumberOfAttributes * 3 / 2, spanData.Attributes.DroppedAttributesCount);
            Assert.Equal(maxNumberOfAttributes, spanData.Attributes.AttributeMap.Count);

            // Test that we still have in the attributes map the latest maxNumberOfAttributes / 2 entries.
            for (int i = 0; i < maxNumberOfAttributes / 2; i++)
            {
                Assert.Equal(
                    AttributeValue.LongAttributeValue(i + (maxNumberOfAttributes * 3 / 2)),
                    spanData.Attributes.AttributeMap["MyStringAttributeKey" + (i + (maxNumberOfAttributes * 3 / 2))]);
            }

            // Test that we have the newest re-added initial entries.
            for (int i = 0; i < maxNumberOfAttributes / 2; i++)
            {
                Assert.Equal(AttributeValue.LongAttributeValue(i), spanData.Attributes.AttributeMap["MyStringAttributeKey" + i]);
            }
        }

        [Fact]
        public void DroppingAnnotations()
        {
            int maxNumberOfAnnotations = 8;
            TraceParams traceParams =
                TraceParams.DEFAULT.ToBuilder().SetMaxNumberOfAnnotations(maxNumberOfAnnotations).Build();
            ISpan span =
                Span.StartSpan(
                    spanContext,
                    recordSpanOptions,
                    SPAN_NAME,
                    parentSpanId,
                    false,
                    traceParams,
                    startEndHandler,
                    timestampConverter,
                    testClock);
            IAnnotation annotation = Annotation.FromDescription(ANNOTATION_DESCRIPTION);
            for (int i = 0; i < 2 * maxNumberOfAnnotations; i++)
            {
                span.AddAnnotation(annotation);
                testClock.AdvanceTime(Duration.Create(0, 100));
            }

            ISpanData spanData = ((Span)span).ToSpanData();
            Assert.Equal(maxNumberOfAnnotations, spanData.Annotations.DroppedEventsCount);
            Assert.Equal(maxNumberOfAnnotations, spanData.Annotations.Events.Count);
            for (int i = 0; i < maxNumberOfAnnotations; i++)
            {
                Assert.Equal(timestamp.AddNanos(100 * (maxNumberOfAnnotations + i)), spanData.Annotations.Events[i].Timestamp);
                Assert.Equal(annotation, spanData.Annotations.Events[i].Event);
            }

            span.End();
            spanData = ((Span)span).ToSpanData();
            Assert.Equal(maxNumberOfAnnotations, spanData.Annotations.DroppedEventsCount);
            Assert.Equal(maxNumberOfAnnotations, spanData.Annotations.Events.Count);
            for (int i = 0; i < maxNumberOfAnnotations; i++)
            {
                Assert.Equal(timestamp.AddNanos(100 * (maxNumberOfAnnotations + i)), spanData.Annotations.Events[i].Timestamp);
                Assert.Equal(annotation, spanData.Annotations.Events[i].Event);
            }
        }

        [Fact]
        public void DroppingNetworkEvents()
        {
            int maxNumberOfNetworkEvents = 8;
            TraceParams traceParams =
                TraceParams.DEFAULT
                    .ToBuilder()
                    .SetMaxNumberOfMessageEvents(maxNumberOfNetworkEvents)
                    .Build();
            ISpan span =
                Span.StartSpan(
                    spanContext,
                    recordSpanOptions,
                    SPAN_NAME,
                    parentSpanId,
                    false,
                    traceParams,
                    startEndHandler,
                    timestampConverter,
                    testClock);
            IMessageEvent networkEvent = MessageEvent.Builder(MessageEventType.RECEIVED, 1).SetUncompressedMessageSize(3).Build();
            for (int i = 0; i < 2 * maxNumberOfNetworkEvents; i++)
            {
                span.AddMessageEvent(networkEvent);
                testClock.AdvanceTime(Duration.Create(0, 100));
            }

            ISpanData spanData = ((Span)span).ToSpanData();
            Assert.Equal(maxNumberOfNetworkEvents, spanData.MessageEvents.DroppedEventsCount);
            Assert.Equal(maxNumberOfNetworkEvents, spanData.MessageEvents.Events.Count);
            for (int i = 0; i < maxNumberOfNetworkEvents; i++)
            {
                Assert.Equal(timestamp.AddNanos(100 * (maxNumberOfNetworkEvents + i)), spanData.MessageEvents.Events[i].Timestamp);
                Assert.Equal(networkEvent, spanData.MessageEvents.Events[i].Event);
            }

            span.End();
            spanData = ((Span)span).ToSpanData();
            Assert.Equal(maxNumberOfNetworkEvents, spanData.MessageEvents.DroppedEventsCount);
            Assert.Equal(maxNumberOfNetworkEvents, spanData.MessageEvents.Events.Count);
            for (int i = 0; i < maxNumberOfNetworkEvents; i++)
            {
                Assert.Equal(timestamp.AddNanos(100 * (maxNumberOfNetworkEvents + i)), spanData.MessageEvents.Events[i].Timestamp);
                Assert.Equal(networkEvent, spanData.MessageEvents.Events[i].Event);
            }
        }

        [Fact]
        public void DroppingLinks()
        {
            int maxNumberOfLinks = 8;
            TraceParams traceParams =
                TraceParams.DEFAULT.ToBuilder().SetMaxNumberOfLinks(maxNumberOfLinks).Build();
            ISpan span =
                Span.StartSpan(
                    spanContext,
                    recordSpanOptions,
                    SPAN_NAME,
                    parentSpanId,
                    false,
                    traceParams,
                    startEndHandler,
                    timestampConverter,
                    testClock);
            ILink link = Link.FromSpanContext(spanContext, LinkType.CHILD_LINKED_SPAN);
            for (int i = 0; i < 2 * maxNumberOfLinks; i++)
            {
                span.AddLink(link);
            }

            ISpanData spanData = ((Span)span).ToSpanData();
            Assert.Equal(maxNumberOfLinks, spanData.Links.DroppedLinksCount);
            Assert.Equal(maxNumberOfLinks, spanData.Links.Links.Count);
            for (int i = 0; i < maxNumberOfLinks; i++)
            {
                Assert.Equal(link, spanData.Links.Links[i]);
            }

            span.End();
            spanData = ((Span)span).ToSpanData();
            Assert.Equal(maxNumberOfLinks, spanData.Links.DroppedLinksCount);
            Assert.Equal(maxNumberOfLinks, spanData.Links.Links.Count);
            for (int i = 0; i < maxNumberOfLinks; i++)
            {
                Assert.Equal(link, spanData.Links.Links[i]);
            }
        }

        [Fact]
        public void SampleToLocalSpanStore()
        {
            ISpan span =
                Span.StartSpan(
                    spanContext,
                    recordSpanOptions,
                    SPAN_NAME,
                    parentSpanId,
                    false,
                    TraceParams.DEFAULT,
                    startEndHandler,
                    timestampConverter,
                    testClock);
            span.End(EndSpanOptions.Builder().SetSampleToLocalSpanStore(true).Build());

            Assert.True(((Span)span).IsSampleToLocalSpanStore);
            ISpan span2 =
                Span.StartSpan(
                    spanContext,
                    recordSpanOptions,
                    SPAN_NAME,
                    parentSpanId,
                    false,
                    TraceParams.DEFAULT,
                    startEndHandler,
                    timestampConverter,
                    testClock);
            span2.End();

            Assert.False(((Span)span2).IsSampleToLocalSpanStore);

            var startEndMock = Mock.Get<IStartEndHandler>(startEndHandler);
            var spanBase = span as SpanBase;
            startEndMock.Verify(s => s.OnEnd(spanBase), Times.Exactly(1));
            var spanBase2 = span2 as SpanBase;
            startEndMock.Verify(s => s.OnEnd(spanBase2), Times.Exactly(1));
        }

        [Fact]
        public void SampleToLocalSpanStore_RunningSpan()
        {
            ISpan span =
                Span.StartSpan(
                    spanContext,
                    recordSpanOptions,
                    SPAN_NAME,
                    parentSpanId,
                    false,
                    TraceParams.DEFAULT,
                    startEndHandler,
                    timestampConverter,
                    testClock);

            Assert.Throws<InvalidOperationException>(() => ((Span)span).IsSampleToLocalSpanStore);
        }
    }
}
