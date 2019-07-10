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
using Steeltoe.Management.Census.Trace.Internal;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    [Obsolete]
    public class SpanBaseTest
    {
        private readonly RandomGenerator random;
        private readonly ISpanContext spanContext;
        private readonly ISpanContext notSampledSpanContext;
        private readonly SpanOptions spanOptions;

        public SpanBaseTest()
        {
            random = new RandomGenerator(1234);
            spanContext =
                SpanContext.Create(
                    TraceId.GenerateRandomId(random),
                    SpanId.GenerateRandomId(random),
                    TraceOptions.Builder().SetIsSampled(true).Build());
            notSampledSpanContext =
                SpanContext.Create(
                    TraceId.GenerateRandomId(random),
                    SpanId.GenerateRandomId(random),
                    TraceOptions.DEFAULT);
            spanOptions = SpanOptions.RECORD_EVENTS;
        }

        [Fact]
        public void NewSpan_WithNullContext()
        {
            Assert.Throws<ArgumentNullException>(() => new NoopSpan(null, default(SpanOptions)));
        }

        [Fact]
        public void GetOptions_WhenNullOptions()
        {
            ISpan span = new NoopSpan(notSampledSpanContext, default(SpanOptions));
            Assert.Equal(SpanOptions.NONE, span.Options);
        }

        [Fact]
        public void GetContextAndOptions()
        {
            ISpan span = new NoopSpan(spanContext, spanOptions);
            Assert.Equal(spanContext, span.Context);
            Assert.Equal(spanOptions, span.Options);
        }

        [Fact]
        public void PutAttributeCallsAddAttributesByDefault()
        {
            var mockSpan = new Mock<NoopSpan>(spanContext, spanOptions) { CallBase = true };
            NoopSpan span = mockSpan.Object;
            IAttributeValue val = AttributeValue<bool>.Create(true);
            span.PutAttribute("MyKey", val);
            span.End();
            mockSpan.Verify((s) => s.PutAttributes(It.Is<IDictionary<string, IAttributeValue>>((d) => d.ContainsKey("MyKey"))));
        }

        [Fact]
        public void EndCallsEndWithDefaultOptions()
        {
            var mockSpan = new Mock<NoopSpan>(spanContext, spanOptions) { CallBase = true };
            var span = mockSpan.Object;
            span.End();
            mockSpan.Verify((s) => s.End(EndSpanOptions.DEFAULT));
        }

        [Fact]
        public void AddMessageEventDefaultImplementation()
        {
            Mock<SpanBase> mockSpan = new Mock<SpanBase>();
            var span = mockSpan.Object;

            IMessageEvent messageEvent =
                MessageEvent.Builder(MessageEventType.SENT, 123)
                    .SetUncompressedMessageSize(456)
                    .SetCompressedMessageSize(789)
                    .Build();

            span.AddMessageEvent(messageEvent);
            mockSpan.Verify((s) => s.AddMessageEvent(messageEvent));
        }
    }
}
