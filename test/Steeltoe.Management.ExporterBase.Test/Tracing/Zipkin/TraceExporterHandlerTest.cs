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

using OpenCensus.Common;
using OpenCensus.Internal;
using OpenCensus.Trace;
using OpenCensus.Trace.Export;
using Steeltoe.Management.Exporter.Tracing.Zipkin;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Steeltoe.Management.Exporter.Tracing.Zipkin.Test
{
    public class TraceExporterHandlerTest
    {
        private const long EPOCH_SECONDS = 62135596800;

        [Fact]
        public void GenerateSpan()
        {
            ZipkinEndpoint localEndpoint = new ZipkinEndpoint()
            {
                ServiceName = "tweetiebird"
            };

            var traceId = "d239036e7d5cec116b562147388b35bf";
            var spanId = "9cc1e3049173be09";
            var parentId = "8b03ab423da481c5";

            Dictionary<string, IAttributeValue> attributes = new Dictionary<string, IAttributeValue>();
            IList<ITimedEvent<IAnnotation>> annotations = new List<ITimedEvent<IAnnotation>>();
            List<ITimedEvent<IMessageEvent>> networkEvents = new List<ITimedEvent<IMessageEvent>>()
            {
                TimedEvent<IMessageEvent>.Create(Timestamp.Create(EPOCH_SECONDS + 1505855799, 433901068), MessageEvent.Builder(MessageEventType.RECEIVED, 0).SetCompressedMessageSize(7).SetUncompressedMessageSize(0).Build()),
                TimedEvent<IMessageEvent>.Create(Timestamp.Create(EPOCH_SECONDS + 1505855799, 459486280), MessageEvent.Builder(MessageEventType.SENT, 0).SetCompressedMessageSize(13).SetUncompressedMessageSize(0).Build())
            };

            ISpanData data = SpanData.Create(
                SpanContext.Create(
                    TraceId.FromBytes(StringToByteArray(traceId)),
                    SpanId.FromBytes(StringToByteArray(spanId)),
                    TraceOptions.FromBytes(new byte[] { 1 })),
                SpanId.FromBytes(StringToByteArray(parentId)),
                true, /* hasRemoteParent */
                "Recv.helloworld.Greeter.SayHello", /* name */
                Timestamp.Create(EPOCH_SECONDS + 1505855794, 194009601) /* startTimestamp */,
                Attributes.Create(attributes, 0 /* droppedAttributesCount */),
                TimedEvents<IAnnotation>.Create(annotations, 0 /* droppedEventsCount */),
                TimedEvents<IMessageEvent>.Create(networkEvents, 0 /* droppedEventsCount */),
                LinkList.Create(new List<ILink>(), 0 /* droppedLinksCount */),
                null, /* childSpanCount */
                Status.Ok,
                Timestamp.Create(EPOCH_SECONDS + 1505855799, 465726528) /* endTimestamp */);

            var handler = new TraceExporterHandler(new TraceExporterOptions() { UseShortTraceIds = false });
            var result = handler.GenerateSpan(data, localEndpoint);

            var zspan = ZipkinSpan.NewBuilder()
                .TraceId(traceId)
                .ParentId(parentId)
                .Id(spanId)
                .Kind(ZipkinSpanKind.SERVER)
                .Name(data.Name)
                .Timestamp(1505855794000000L + (194009601L / 1000))
                .Duration(
                    (1505855799000000L + (465726528L / 1000))
                        - (1505855794000000L + (194009601L / 1000)))
                .LocalEndpoint(localEndpoint)
                .AddAnnotation(1505855799000000L + (433901068L / 1000), "RECEIVED")
                .AddAnnotation(1505855799000000L + (459486280L / 1000), "SENT")
                .PutTag("census.status_code", "Ok")
                .Build();

            Assert.Equal(zspan, result);
        }

        [Fact]
        public void GenerateSpan_ShortTraceId()
        {
            ZipkinEndpoint localEndpoint = new ZipkinEndpoint()
            {
                ServiceName = "tweetiebird"
            };

            var traceId = "00000000000000006b562147388b35bf";
            var shorttraceId = "6b562147388b35bf";
            var spanId = "9cc1e3049173be09";
            var parentId = "8b03ab423da481c5";

            Dictionary<string, IAttributeValue> attributes = new Dictionary<string, IAttributeValue>();
            IList<ITimedEvent<IAnnotation>> annotations = new List<ITimedEvent<IAnnotation>>();
            List<ITimedEvent<IMessageEvent>> networkEvents = new List<ITimedEvent<IMessageEvent>>()
            {
                TimedEvent<IMessageEvent>.Create(Timestamp.Create(EPOCH_SECONDS + 1505855799, 433901068), MessageEvent.Builder(MessageEventType.RECEIVED, 0).SetCompressedMessageSize(7).SetUncompressedMessageSize(0).Build()),
                TimedEvent<IMessageEvent>.Create(Timestamp.Create(EPOCH_SECONDS + 1505855799, 459486280), MessageEvent.Builder(MessageEventType.SENT, 0).SetCompressedMessageSize(13).SetUncompressedMessageSize(0).Build())
            };

            ISpanData data = SpanData.Create(
                SpanContext.Create(
                    TraceId.FromBytes(StringToByteArray(traceId)),
                    SpanId.FromBytes(StringToByteArray(spanId)),
                    TraceOptions.FromBytes(new byte[] { 1 })),
                SpanId.FromBytes(StringToByteArray(parentId)),
                true, /* hasRemoteParent */
                "Recv.helloworld.Greeter.SayHello", /* name */
                Timestamp.Create(EPOCH_SECONDS + 1505855794, 194009601) /* startTimestamp */,
                Attributes.Create(attributes, 0 /* droppedAttributesCount */),
                TimedEvents<IAnnotation>.Create(annotations, 0 /* droppedEventsCount */),
                TimedEvents<IMessageEvent>.Create(networkEvents, 0 /* droppedEventsCount */),
                LinkList.Create(new List<ILink>(), 0 /* droppedLinksCount */),
                null, /* childSpanCount */
                Status.Ok,
                Timestamp.Create(EPOCH_SECONDS + 1505855799, 465726528) /* endTimestamp */);

            var handler = new TraceExporterHandler(new TraceExporterOptions());
            var result = handler.GenerateSpan(data, localEndpoint);

            var zspan = ZipkinSpan.NewBuilder()
                .TraceId(shorttraceId)
                .ParentId(parentId)
                .Id(spanId)
                .Kind(ZipkinSpanKind.SERVER)
                .Name(data.Name)
                .Timestamp(1505855794000000L + (194009601L / 1000))
                .Duration(
                    (1505855799000000L + (465726528L / 1000))
                        - (1505855794000000L + (194009601L / 1000)))
                .LocalEndpoint(localEndpoint)
                .AddAnnotation(1505855799000000L + (433901068L / 1000), "RECEIVED")
                .AddAnnotation(1505855799000000L + (459486280L / 1000), "SENT")
                .PutTag("census.status_code", "Ok")
                .Build();

            Assert.Equal(zspan, result);
        }

        internal static byte[] StringToByteArray(string src)
        {
            int size = src.Length / 2;
            byte[] bytes = new byte[size];
            for (int i = 0, j = 0; i < size; i++)
            {
                int high = HexCharToInt(src[j++]);
                int low = HexCharToInt(src[j++]);
                bytes[i] = (byte)(high << 4 | low);
            }

            return bytes;
        }

        internal static int HexCharToInt(char c)
        {
            if ((c >= '0') && (c <= '9'))
            {
                return c - '0';
            }

            if ((c >= 'a') && (c <= 'f'))
            {
                return (c - 'a') + 10;
            }

            if ((c >= 'A') && (c <= 'F'))
            {
                return (c - 'A') + 10;
            }

            throw new ArgumentOutOfRangeException("Invalid character: " + c);
        }
    }
}
