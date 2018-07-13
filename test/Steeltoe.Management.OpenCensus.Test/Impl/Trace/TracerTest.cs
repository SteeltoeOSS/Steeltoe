using Moq;
using Steeltoe.Management.Census.Testing.Common;
using Steeltoe.Management.Census.Trace.Config;
using Steeltoe.Management.Census.Trace.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{

    public class TracerTest
    {
        private const string SPAN_NAME = "MySpanName";
        private IStartEndHandler startEndHandler;
        private ITraceConfig traceConfig;
        private Tracer tracer;


        public TracerTest()
        {
            startEndHandler = Mock.Of<IStartEndHandler>();
            traceConfig = Mock.Of<ITraceConfig>();
            tracer = new Tracer(new RandomGenerator(), startEndHandler, TestClock.Create(), traceConfig);
        }

        [Fact]
        public void CreateSpanBuilder()
        {
            ISpanBuilder spanBuilder = tracer.SpanBuilderWithExplicitParent(SPAN_NAME, BlankSpan.INSTANCE);
            Assert.IsType<SpanBuilder>(spanBuilder);
        }

        [Fact]
        public void CreateSpanBuilderWithRemoteParet()
        {
            ISpanBuilder spanBuilder = tracer.SpanBuilderWithRemoteParent(SPAN_NAME, SpanContext.INVALID);
            Assert.IsType<SpanBuilder>(spanBuilder);
        }
    }
}
