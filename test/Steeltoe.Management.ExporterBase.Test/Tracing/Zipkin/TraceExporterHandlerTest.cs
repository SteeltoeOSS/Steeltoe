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

using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Internal;
using Steeltoe.Management.Census.Trace;
using Steeltoe.Management.Census.Trace.Export;
using Steeltoe.Management.Census.Utils;
using Steeltoe.Management.Exporter.Tracing.Zipkin;
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
                TimedEvent<IMessageEvent>.Create(Timestamp.Create(EPOCH_SECONDS + 1505855799, 433901068), new MessageEventBuilder(MessageEventType.RECEIVED, 0, 0, 0).SetCompressedMessageSize(7).Build()),
                TimedEvent<IMessageEvent>.Create(Timestamp.Create(EPOCH_SECONDS + 1505855799, 459486280), new MessageEventBuilder(MessageEventType.SENT, 0, 0, 0).SetCompressedMessageSize(13).Build())
            };

            ISpanData data = SpanData.Create(
                SpanContext.Create(
                    TraceId.FromBytes(Arrays.StringToByteArray(traceId)),
                    SpanId.FromBytes(Arrays.StringToByteArray(spanId)),
                    TraceOptions.FromBytes(new byte[] { 1 })),
                SpanId.FromBytes(Arrays.StringToByteArray(parentId)),
                true, /* hasRemoteParent */
                "Recv.helloworld.Greeter.SayHello", /* name */
                Timestamp.Create(EPOCH_SECONDS + 1505855794, 194009601) /* startTimestamp */,
                Attributes.Create(attributes, 0 /* droppedAttributesCount */),
                TimedEvents<IAnnotation>.Create(annotations, 0 /* droppedEventsCount */),
                TimedEvents<IMessageEvent>.Create(networkEvents, 0 /* droppedEventsCount */),
                LinkList.Create(new List<ILink>(), 0 /* droppedLinksCount */),
                null, /* childSpanCount */
                Status.OK,
                Timestamp.Create(EPOCH_SECONDS + 1505855799, 465726528) /* endTimestamp */);

            var handler = new TraceExporterHandler(new TraceExporterOptions());
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
                .PutTag("census.status_code", "OK")
                .Build();

            Assert.Equal(zspan, result);
        }
    }
}
