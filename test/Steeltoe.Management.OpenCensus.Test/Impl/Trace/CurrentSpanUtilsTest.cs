using Moq;
using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Trace.Internal;
using Steeltoe.Management.Census.Trace.Unsafe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    public class CurrentSpanUtilsTest
    {
        private ISpan span;
        private RandomGenerator random;
        private ISpanContext spanContext;
        private SpanOptions spanOptions;

        public CurrentSpanUtilsTest()
        {
            random = new RandomGenerator(1234);
            spanContext =
                SpanContext.Create(
                    TraceId.GenerateRandomId(random),
                    SpanId.GenerateRandomId(random),
                    TraceOptions.Builder().SetIsSampled(true).Build());

            spanOptions = SpanOptions.RECORD_EVENTS;
            var mockSpan = new Mock<NoopSpan>(spanContext, spanOptions) { CallBase = true };
            span = mockSpan.Object;
        }

        [Fact]
        public void CurrentSpan_WhenNoContext()
        {
            Assert.Null(CurrentSpanUtils.CurrentSpan);
        }

        [Fact]
        public void WithSpan_CloseDetaches()
        {
            Assert.Null(CurrentSpanUtils.CurrentSpan);
            IScope ws = CurrentSpanUtils.WithSpan(span, false);
            try
            {
                Assert.Same(span, CurrentSpanUtils.CurrentSpan);
            }
            finally
            {
                ws.Dispose();
            }
            Assert.Null(CurrentSpanUtils.CurrentSpan);
        }

        [Fact]
        public void WithSpan_CloseDetachesAndEndsSpan()
        {
            Assert.Null(CurrentSpanUtils.CurrentSpan);
            IScope ss = CurrentSpanUtils.WithSpan(span, true);
            try
            {
                Assert.Same(span, CurrentSpanUtils.CurrentSpan);
            }
            finally
            {
                ss.Dispose();
            }
            Assert.Null(CurrentSpanUtils.CurrentSpan);
            Mock.Get<ISpan>(span).Verify((s) => s.End(EndSpanOptions.DEFAULT));
        }
    }
}
