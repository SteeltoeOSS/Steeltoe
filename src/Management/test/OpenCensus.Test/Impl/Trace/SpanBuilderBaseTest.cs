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
using System;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    [Obsolete]
    public class SpanBuilderBaseTest
    {
        private ITracer tracer;
        private Mock<SpanBuilderBase> spanBuilder = new Mock<SpanBuilderBase>();
        private Mock<SpanBase> span = new Mock<SpanBase>();

        public SpanBuilderBaseTest()
        {
            tracer = Tracing.Tracer;
            spanBuilder.Setup((b) => b.StartSpan()).Returns(span.Object);
        }

        [Fact]
        public void StartScopedSpan()
        {
            Assert.Same(BlankSpan.INSTANCE, tracer.CurrentSpan);
            IScope scope = spanBuilder.Object.StartScopedSpan();
            try
            {
                Assert.Same(span.Object, tracer.CurrentSpan);
            }
            finally
            {
                scope.Dispose();
            }

            span.Verify(s => s.End(EndSpanOptions.DEFAULT));
            Assert.Same(BlankSpan.INSTANCE, tracer.CurrentSpan);
        }

        // [Fact]
        // public void StartSpanAndRun()
        //      {
        //          assertThat(tracer.getCurrentSpan()).isSameAs(BlankSpan.INSTANCE);
        //          spanBuilder.startSpanAndRun(
        //              new Runnable() {
        //        @Override
        //                public void run()
        //          {
        //              assertThat(tracer.getCurrentSpan()).isSameAs(span);
        //          }
        //      });
        //  verify(span).end(EndSpanOptions.DEFAULT);
        //      assertThat(tracer.getCurrentSpan()).isSameAs(BlankSpan.INSTANCE);
        //  }

        // [Fact]
        //  public void StartSpanAndCall() throws Exception
        //    {
        //        final Object ret = new Object();
        //    assertThat(tracer.getCurrentSpan()).isSameAs(BlankSpan.INSTANCE);
        //    assertThat(
        //            spanBuilder.startSpanAndCall(
        //                new Callable<Object>() {
        //                  @Override
        //                  public Object call() throws Exception
        //    {
        //        assertThat(tracer.getCurrentSpan()).isSameAs(span);
        //                    return ret;
        //    }
        // }))
        //        .isEqualTo(ret);
        // verify(span).end(EndSpanOptions.DEFAULT);
        // assertThat(tracer.getCurrentSpan()).isSameAs(BlankSpan.INSTANCE);
        //  }
    }
}
