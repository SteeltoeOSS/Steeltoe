using Moq;
using Steeltoe.Management.Census.Trace.Internal;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    public class SpanBaseTest
    {
        private RandomGenerator random;
        private ISpanContext spanContext;
        private ISpanContext notSampledSpanContext;
        private SpanOptions spanOptions;

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
