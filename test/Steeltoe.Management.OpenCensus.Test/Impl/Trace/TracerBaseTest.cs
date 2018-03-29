using Moq;
using Steeltoe.Management.Census.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    public class TracerBaseTest
    {
        private static readonly ITracer noopTracer = TracerBase.NoopTracer;
        private static readonly string SPAN_NAME = "MySpanName";
        private TracerBase tracer = Mock.Of<TracerBase>();
        private SpanBuilderBase spanBuilder = Mock.Of<SpanBuilderBase>();
        private SpanBase span = Mock.Of<SpanBase>();

        public TracerBaseTest()
        {
        }

        [Fact]
        public void DefaultGetCurrentSpan()
        {
            Assert.Equal(BlankSpan.INSTANCE, noopTracer.CurrentSpan);
        }

        [Fact]
        public void WithSpan_NullSpan()
        {
            Assert.Throws<ArgumentNullException>(() => noopTracer.WithSpan(null));
        }

        [Fact]
        public void GetCurrentSpan_WithSpan()
        {
            Assert.Same(BlankSpan.INSTANCE, noopTracer.CurrentSpan);
            IScope ws = noopTracer.WithSpan(span);
            try
            {
                Assert.Same(span, noopTracer.CurrentSpan);
            }
            finally
            {
                ws.Dispose();
            }
            Assert.Same(BlankSpan.INSTANCE, noopTracer.CurrentSpan);
        }

        //      [Fact]
        //public void wrapRunnable()
        //      {
        //          Runnable runnable;
        //          Assert.Equal(noopTracer.getCurrentSpan()).isSameAs(BlankSpan.INSTANCE);
        //          runnable =
        //              tracer.withSpan(
        //                  span,
        //                  new Runnable() {
        //            @Override
        //                    public void run()
        //          {
        //              Assert.Equal(noopTracer.getCurrentSpan()).isSameAs(span);
        //          }
        //      });
        //  // When we run the runnable we will have the span in the current Context.
        //  runnable.run();
        //  verifyZeroInteractions(span);
        //      Assert.Equal(noopTracer.getCurrentSpan()).isSameAs(BlankSpan.INSTANCE);
        //  }

        //   [Fact]
        //  public void wrapCallable() throws Exception
        //    {
        //        readonly Object ret = new Object();
        //    Callable<Object> callable;
        //    Assert.Equal(noopTracer.getCurrentSpan()).isSameAs(BlankSpan.INSTANCE);
        //    callable =
        //        tracer.withSpan(
        //            span,
        //            new Callable<Object>() {
        //              @Override
        //              public Object call() throws Exception
        //    {
        //        Assert.Equal(noopTracer.getCurrentSpan()).isSameAs(span);
        //                return ret;
        //    }
        //});
        //    // When we call the callable we will have the span in the current Context.
        //    Assert.Equal(callable.call()).isEqualTo(ret);
        //verifyZeroInteractions(span);
        //Assert.Equal(noopTracer.getCurrentSpan()).isSameAs(BlankSpan.INSTANCE);
        //  }

        [Fact]
        public void SpanBuilderWithName_NullName()
        {
            Assert.Throws<ArgumentNullException>(() => noopTracer.SpanBuilder(null));
        }

        [Fact]
        public void DefaultSpanBuilderWithName()
        {
            Assert.Same(BlankSpan.INSTANCE, noopTracer.SpanBuilder(SPAN_NAME).StartSpan());
        }

        [Fact]
        public void SpanBuilderWithParentAndName_NullName()
        {
            Assert.Throws<ArgumentNullException>(() => noopTracer.SpanBuilderWithExplicitParent(null, null));
        }

        [Fact]
        public void DefaultSpanBuilderWithParentAndName()
        {
            Assert.Same(BlankSpan.INSTANCE, noopTracer.SpanBuilderWithExplicitParent(SPAN_NAME, null).StartSpan());
        }

        [Fact]
        public void spanBuilderWithRemoteParent_NullName()
        {
            Assert.Throws<ArgumentNullException>(() => noopTracer.SpanBuilderWithRemoteParent(null, null));
        }

        [Fact]
        public void DefaultSpanBuilderWithRemoteParent_NullParent()
        {
            Assert.Same(BlankSpan.INSTANCE, noopTracer.SpanBuilderWithRemoteParent(SPAN_NAME, null).StartSpan());
        }

        [Fact]
        public void DefaultSpanBuilderWithRemoteParent()
        {
            Assert.Same(BlankSpan.INSTANCE, noopTracer.SpanBuilderWithRemoteParent(SPAN_NAME, SpanContext.INVALID).StartSpan());
        }

        [Fact]
        public void StartSpanWithParentFromContext()
        {
            IScope ws = tracer.WithSpan(span);
            try
            {
                Assert.Same(span, tracer.CurrentSpan);
                Mock.Get(tracer).Setup((tracer) => tracer.SpanBuilderWithExplicitParent(SPAN_NAME, span)).Returns(spanBuilder);
                Assert.Same(spanBuilder, tracer.SpanBuilder(SPAN_NAME));
            }
            finally
            {
                ws.Dispose();
            }
        }

        [Fact]
        public void StartSpanWithInvalidParentFromContext()
        {
            IScope ws = tracer.WithSpan(BlankSpan.INSTANCE);
            try
            {
                Assert.Same(BlankSpan.INSTANCE, tracer.CurrentSpan);
                Mock.Get(tracer).Setup((t) => t.SpanBuilderWithExplicitParent(SPAN_NAME, BlankSpan.INSTANCE)).Returns(spanBuilder);
                Assert.Same(spanBuilder, tracer.SpanBuilder(SPAN_NAME));
            }
            finally
            {
                ws.Dispose();
            }
        }
    }
}
