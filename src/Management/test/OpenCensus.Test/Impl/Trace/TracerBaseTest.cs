﻿// <copyright file="TracerBaseTest.cs" company="OpenCensus Authors">
// Copyright 2018, OpenCensus Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenCensus.Trace.Test
{
    using System;
    using Internal;
    using Moq;
    using OpenCensus.Common;
    using Xunit;

    public class TracerBaseTest
    {
        private static readonly ITracer noopTracer = TracerBase.NoopTracer;
        private static readonly string SPAN_NAME = "MySpanName";
        private readonly TracerBase tracer = Mock.Of<TracerBase>();
        private readonly SpanBuilderBase spanBuilder = Mock.Of<SpanBuilderBase>();
        private readonly SpanBase span = Mock.Of<SpanBase>();

        public TracerBaseTest()
        {
        }

        [Fact]
        public void DefaultGetCurrentSpan()
        {
            Assert.Equal(BlankSpan.Instance, noopTracer.CurrentSpan);
        }

        [Fact]
        public void WithSpan_NullSpan()
        {
            Assert.Throws<ArgumentNullException>(() => noopTracer.WithSpan(null));
        }

        [Fact]
        public void GetCurrentSpan_WithSpan()
        {
            Assert.Same(BlankSpan.Instance, noopTracer.CurrentSpan);
            var ws = noopTracer.WithSpan(span);
            try
            {
                Assert.Same(span, noopTracer.CurrentSpan);
            }
            finally
            {
                ws.Dispose();
            }
            Assert.Same(BlankSpan.Instance, noopTracer.CurrentSpan);
        }

        // [Fact]
        // public void wrapRunnable()
        //      {
        //          Runnable runnable;
        //          Assert.Equal(noopTracer.getCurrentSpan()).isSameAs(BlankSpan.Instance);
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
        //      Assert.Equal(noopTracer.getCurrentSpan()).isSameAs(BlankSpan.Instance);
        //  }

        // [Fact]
        //  public void wrapCallable() throws Exception
        //    {
        //        readonly Object ret = new Object();
        //    Callable<Object> callable;
        //    Assert.Equal(noopTracer.getCurrentSpan()).isSameAs(BlankSpan.Instance);
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
        // });
        //    // When we call the callable we will have the span in the current Context.
        //    Assert.Equal(callable.call()).isEqualTo(ret);
        // verifyZeroInteractions(span);
        // Assert.Equal(noopTracer.getCurrentSpan()).isSameAs(BlankSpan.Instance);
        //  }

        [Fact]
        public void SpanBuilderWithName_NullName()
        {
            Assert.Throws<ArgumentNullException>(() => noopTracer.SpanBuilder(null));
        }

        [Fact]
        public void DefaultSpanBuilderWithName()
        {
            Assert.Same(BlankSpan.Instance, noopTracer.SpanBuilder(SPAN_NAME).StartSpan());
        }

        [Fact]
        public void SpanBuilderWithParentAndName_NullName()
        {
            Assert.Throws<ArgumentNullException>(() => noopTracer.SpanBuilderWithExplicitParent(null, null));
        }

        [Fact]
        public void DefaultSpanBuilderWithParentAndName()
        {
            Assert.Same(BlankSpan.Instance, noopTracer.SpanBuilderWithExplicitParent(SPAN_NAME, null).StartSpan());
        }

        [Fact]
        public void spanBuilderWithRemoteParent_NullName()
        {
            Assert.Throws<ArgumentNullException>(() => noopTracer.SpanBuilderWithRemoteParent(null, null));
        }

        [Fact]
        public void DefaultSpanBuilderWithRemoteParent_NullParent()
        {
            Assert.Same(BlankSpan.Instance, noopTracer.SpanBuilderWithRemoteParent(SPAN_NAME, null).StartSpan());
        }

        [Fact]
        public void DefaultSpanBuilderWithRemoteParent()
        {
            Assert.Same(BlankSpan.Instance, noopTracer.SpanBuilderWithRemoteParent(SPAN_NAME, SpanContext.Invalid).StartSpan());
        }

        [Fact]
        public void StartSpanWithParentFromContext()
        {
            var ws = tracer.WithSpan(span);
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
            var ws = tracer.WithSpan(BlankSpan.Instance);
            try
            {
                Assert.Same(BlankSpan.Instance, tracer.CurrentSpan);
                Mock.Get(tracer).Setup((t) => t.SpanBuilderWithExplicitParent(SPAN_NAME, BlankSpan.Instance)).Returns(spanBuilder);
                Assert.Same(spanBuilder, tracer.SpanBuilder(SPAN_NAME));
            }
            finally
            {
                ws.Dispose();
            }
        }
    }
}