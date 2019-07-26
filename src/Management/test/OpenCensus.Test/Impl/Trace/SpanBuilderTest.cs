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
using Steeltoe.Management.Census.Testing.Common;
using Steeltoe.Management.Census.Trace.Config;
using Steeltoe.Management.Census.Trace.Export;
using Steeltoe.Management.Census.Trace.Internal;
using Steeltoe.Management.Census.Trace.Sampler;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    [Obsolete]
    public class SpanBuilderTest
    {
        private static readonly string SPAN_NAME = "MySpanName";
        private readonly SpanBuilderOptions spanBuilderOptions;
        private readonly TraceParams alwaysSampleTraceParams = TraceParams.DEFAULT.ToBuilder().SetSampler(Samplers.AlwaysSample).Build();
#pragma warning disable SA1214 // Readonly fields must appear before non-readonly fields
        private readonly TestClock testClock = TestClock.Create();
#pragma warning restore SA1214 // Readonly fields must appear before non-readonly fields
        private readonly IRandomGenerator randomHandler = new FakeRandomHandler();
        private readonly IStartEndHandler startEndHandler = Mock.Of<IStartEndHandler>();
        private readonly ITraceConfig traceConfig = Mock.Of<ITraceConfig>();

        public SpanBuilderTest()
        {
            // MockitoAnnotations.initMocks(this);
            spanBuilderOptions =
                new SpanBuilderOptions(randomHandler, startEndHandler, testClock, traceConfig);
            var configMock = Mock.Get<ITraceConfig>(traceConfig);
            configMock.Setup((c) => c.ActiveTraceParams).Returns(alwaysSampleTraceParams);

            // when(traceConfig.getActiveTraceParams()).thenReturn(alwaysSampleTraceParams);
        }

        [Fact]
        public void StartSpanNullParent()
        {
            ISpan span =
                SpanBuilder.CreateWithParent(SPAN_NAME, null, spanBuilderOptions).StartSpan();
            Assert.True(span.Context.IsValid);
            Assert.True(span.Options.HasFlag(SpanOptions.RECORD_EVENTS));
            Assert.True(span.Context.TraceOptions.IsSampled);
            ISpanData spanData = ((Span)span).ToSpanData();
            Assert.Null(spanData.ParentSpanId);
            Assert.False(spanData.HasRemoteParent);
            Assert.Equal(testClock.Now, spanData.StartTimestamp);
            Assert.Equal(SPAN_NAME, spanData.Name);
        }

        [Fact]
        public void StartSpanNullParentWithRecordEvents()
        {
            ISpan span =
                SpanBuilder.CreateWithParent(SPAN_NAME, null, spanBuilderOptions)
                    .SetSampler(Samplers.NeverSample)
                    .SetRecordEvents(true)
                    .StartSpan();
            Assert.True(span.Context.IsValid);
            Assert.True(span.Options.HasFlag(SpanOptions.RECORD_EVENTS));
            Assert.False(span.Context.TraceOptions.IsSampled);
            ISpanData spanData = ((Span)span).ToSpanData();
            Assert.Null(spanData.ParentSpanId);
            Assert.False(spanData.HasRemoteParent);
        }

        [Fact]
        public void StartSpanNullParentNoRecordOptions()
        {
            ISpan span =
                SpanBuilder.CreateWithParent(SPAN_NAME, null, spanBuilderOptions)
                    .SetSampler(Samplers.NeverSample)
                    .StartSpan();
            Assert.True(span.Context.IsValid);
            Assert.False(span.Options.HasFlag(SpanOptions.RECORD_EVENTS));
            Assert.False(span.Context.TraceOptions.IsSampled);
        }

        [Fact]
        public void StartChildSpan()
        {
            ISpan rootSpan =
                SpanBuilder.CreateWithParent(SPAN_NAME, null, spanBuilderOptions).StartSpan();
            Assert.True(rootSpan.Context.IsValid);
            Assert.True(rootSpan.Options.HasFlag(SpanOptions.RECORD_EVENTS));
            Assert.True(rootSpan.Context.TraceOptions.IsSampled);
            Assert.False(((Span)rootSpan).ToSpanData().HasRemoteParent);
            ISpan childSpan =
                SpanBuilder.CreateWithParent(SPAN_NAME, rootSpan, spanBuilderOptions).StartSpan();
            Assert.True(childSpan.Context.IsValid);
            Assert.Equal(rootSpan.Context.TraceId, childSpan.Context.TraceId);
            Assert.Equal(rootSpan.Context.SpanId, ((Span)childSpan).ToSpanData().ParentSpanId);
            Assert.False(((Span)childSpan).ToSpanData().HasRemoteParent);
            Assert.Equal(((Span)rootSpan).TimestampConverter, ((Span)childSpan).TimestampConverter);
        }

        [Fact]
        public void StartRemoteSpan_NullParent()
        {
            ISpan span =
                SpanBuilder.CreateWithRemoteParent(SPAN_NAME, null, spanBuilderOptions).StartSpan();
            Assert.True(span.Context.IsValid);
            Assert.True(span.Options.HasFlag(SpanOptions.RECORD_EVENTS));
            Assert.True(span.Context.TraceOptions.IsSampled);
            ISpanData spanData = ((Span)span).ToSpanData();
            Assert.Null(spanData.ParentSpanId);
            Assert.False(spanData.HasRemoteParent);
        }

        [Fact]
        public void StartRemoteSpanInvalidParent()
        {
            ISpan span =
                SpanBuilder.CreateWithRemoteParent(SPAN_NAME, SpanContext.INVALID, spanBuilderOptions)
                    .StartSpan();
            Assert.True(span.Context.IsValid);
            Assert.True(span.Options.HasFlag(SpanOptions.RECORD_EVENTS));
            Assert.True(span.Context.TraceOptions.IsSampled);
            ISpanData spanData = ((Span)span).ToSpanData();
            Assert.Null(spanData.ParentSpanId);
            Assert.False(spanData.HasRemoteParent);
        }

        [Fact]
        public void StartRemoteSpan()
        {
            ISpanContext spanContext =
                SpanContext.Create(
                    TraceId.GenerateRandomId(randomHandler),
                    SpanId.GenerateRandomId(randomHandler),
                    TraceOptions.DEFAULT);
            ISpan span =
                SpanBuilder.CreateWithRemoteParent(SPAN_NAME, spanContext, spanBuilderOptions)
                    .StartSpan();
            Assert.True(span.Context.IsValid);
            Assert.Equal(spanContext.TraceId, span.Context.TraceId);
            Assert.True(span.Context.TraceOptions.IsSampled);
            ISpanData spanData = ((Span)span).ToSpanData();
            Assert.Equal(spanContext.SpanId, spanData.ParentSpanId);
            Assert.True(spanData.HasRemoteParent);
        }

        [Fact]
        public void StartRootSpan_WithSpecifiedSampler()
        {
            // Apply given sampler before default sampler for root spans.
            ISpan rootSpan =
                SpanBuilder.CreateWithParent(SPAN_NAME, null, spanBuilderOptions)
                    .SetSampler(Samplers.NeverSample)
                    .StartSpan();
            Assert.True(rootSpan.Context.IsValid);
            Assert.False(rootSpan.Context.TraceOptions.IsSampled);
        }

        [Fact]
        public void StartRootSpan_WithoutSpecifiedSampler()
        {
            // Apply default sampler (always true in the tests) for root spans.
            ISpan rootSpan =
                SpanBuilder.CreateWithParent(SPAN_NAME, null, spanBuilderOptions).StartSpan();
            Assert.True(rootSpan.Context.IsValid);
            Assert.True(rootSpan.Context.TraceOptions.IsSampled);
        }

        [Fact]
        public void StartRemoteChildSpan_WithSpecifiedSampler()
        {
            ISpan rootSpan =
                SpanBuilder.CreateWithParent(SPAN_NAME, null, spanBuilderOptions)
                    .SetSampler(Samplers.AlwaysSample)
                    .StartSpan();
            Assert.True(rootSpan.Context.IsValid);
            Assert.True(rootSpan.Context.TraceOptions.IsSampled);

            // Apply given sampler before default sampler for spans with remote parent.
            ISpan childSpan =
                SpanBuilder.CreateWithRemoteParent(SPAN_NAME, rootSpan.Context, spanBuilderOptions)
                    .SetSampler(Samplers.NeverSample)
                    .StartSpan();
            Assert.True(childSpan.Context.IsValid);
            Assert.Equal(rootSpan.Context.TraceId, childSpan.Context.TraceId);
            Assert.False(childSpan.Context.TraceOptions.IsSampled);
        }

        [Fact]
        public void StartRemoteChildSpan_WithoutSpecifiedSampler()
        {
            ISpan rootSpan =
                SpanBuilder.CreateWithParent(SPAN_NAME, null, spanBuilderOptions)
                    .SetSampler(Samplers.NeverSample)
                    .StartSpan();
            Assert.True(rootSpan.Context.IsValid);
            Assert.False(rootSpan.Context.TraceOptions.IsSampled);

            // Apply default sampler (always true in the tests) for spans with remote parent.
            ISpan childSpan =
                SpanBuilder.CreateWithRemoteParent(SPAN_NAME, rootSpan.Context, spanBuilderOptions)
                    .StartSpan();
            Assert.True(childSpan.Context.IsValid);
            Assert.Equal(rootSpan.Context.TraceId, childSpan.Context.TraceId);
            Assert.True(childSpan.Context.TraceOptions.IsSampled);
        }

        [Fact]
        public void StartChildSpan_WithSpecifiedSampler()
        {
            ISpan rootSpan =
                SpanBuilder.CreateWithParent(SPAN_NAME, null, spanBuilderOptions)
                    .SetSampler(Samplers.AlwaysSample)
                    .StartSpan();
            Assert.True(rootSpan.Context.IsValid);
            Assert.True(rootSpan.Context.TraceOptions.IsSampled);

            // Apply the given sampler for child spans.
            ISpan childSpan =
                SpanBuilder.CreateWithParent(SPAN_NAME, rootSpan, spanBuilderOptions)
                    .SetSampler(Samplers.NeverSample)
                    .StartSpan();
            Assert.True(childSpan.Context.IsValid);
            Assert.Equal(rootSpan.Context.TraceId, childSpan.Context.TraceId);
            Assert.False(childSpan.Context.TraceOptions.IsSampled);
        }

        [Fact]
        public void StartChildSpan_WithoutSpecifiedSampler()
        {
            ISpan rootSpan =
                SpanBuilder.CreateWithParent(SPAN_NAME, null, spanBuilderOptions)
                    .SetSampler(Samplers.NeverSample)
                    .StartSpan();
            Assert.True(rootSpan.Context.IsValid);
            Assert.False(rootSpan.Context.TraceOptions.IsSampled);

            // Don't apply the default sampler (always true) for child spans.
            ISpan childSpan =
                SpanBuilder.CreateWithParent(SPAN_NAME, rootSpan, spanBuilderOptions).StartSpan();
            Assert.True(childSpan.Context.IsValid);
            Assert.Equal(rootSpan.Context.TraceId, childSpan.Context.TraceId);
            Assert.False(childSpan.Context.TraceOptions.IsSampled);
        }

        [Fact]
        public void StartChildSpan_SampledLinkedParent()
        {
            ISpan rootSpanUnsampled =
                SpanBuilder.CreateWithParent(SPAN_NAME, null, spanBuilderOptions)
                    .SetSampler(Samplers.NeverSample)
                    .StartSpan();
            Assert.False(rootSpanUnsampled.Context.TraceOptions.IsSampled);
            ISpan rootSpanSampled =
                SpanBuilder.CreateWithParent(SPAN_NAME, null, spanBuilderOptions)
                    .SetSampler(Samplers.AlwaysSample)
                    .StartSpan();
            Assert.True(rootSpanSampled.Context.TraceOptions.IsSampled);

            // Sampled because the linked parent is sampled.
            ISpan childSpan =
                SpanBuilder.CreateWithParent(SPAN_NAME, rootSpanUnsampled, spanBuilderOptions)
                    .SetParentLinks(new List<ISpan>() { rootSpanSampled })
                    .StartSpan();
            Assert.True(childSpan.Context.IsValid);
            Assert.Equal(rootSpanUnsampled.Context.TraceId, childSpan.Context.TraceId);
            Assert.True(childSpan.Context.TraceOptions.IsSampled);
        }

        [Fact]
        public void StartRemoteChildSpan_WithProbabilitySamplerDefaultSampler()
        {
            var configMock = Mock.Get<ITraceConfig>(traceConfig);
            configMock.Setup((c) => c.ActiveTraceParams).Returns(TraceParams.DEFAULT);

            // This traceId will not be sampled by the ProbabilitySampler because the first 8 bytes as long
            // is not less than probability * Long.MAX_VALUE;
            ITraceId traceId =
                TraceId.FromBytes(
                    new byte[]
                    {
              (byte)0x8F,
              (byte)0xFF,
              (byte)0xFF,
              (byte)0xFF,
              (byte)0xFF,
              (byte)0xFF,
              (byte)0xFF,
              (byte)0xFF,
              0,
              0,
              0,
              0,
              0,
              0,
              0,
              0
                    });

            // If parent is sampled then the remote child must be sampled.
            ISpan childSpan =
                SpanBuilder.CreateWithRemoteParent(
                        SPAN_NAME,
                        SpanContext.Create(
                            traceId,
                            SpanId.GenerateRandomId(randomHandler),
                            TraceOptions.Builder().SetIsSampled(true).Build()),
                        spanBuilderOptions)
                    .StartSpan();
            Assert.True(childSpan.Context.IsValid);
            Assert.Equal(traceId, childSpan.Context.TraceId);
            Assert.True(childSpan.Context.TraceOptions.IsSampled);
            childSpan.End();

            Assert.Equal(TraceParams.DEFAULT, traceConfig.ActiveTraceParams);

            // If parent is not sampled then the remote child must be not sampled.
            childSpan =
                SpanBuilder.CreateWithRemoteParent(
                        SPAN_NAME,
                        SpanContext.Create(
                            traceId,
                            SpanId.GenerateRandomId(randomHandler),
                            TraceOptions.DEFAULT),
                        spanBuilderOptions)
                    .StartSpan();
            Assert.True(childSpan.Context.IsValid);
            Assert.Equal(traceId, childSpan.Context.TraceId);
            Assert.False(childSpan.Context.TraceOptions.IsSampled);
            childSpan.End();
        }

        private class FakeRandomHandler : IRandomGenerator
        {
            private readonly Random random;

            public FakeRandomHandler()
            {
                this.random = new Random(1234);
            }

            public Random Current()
            {
                return random;
            }

            public void NextBytes(byte[] bytes)
            {
                random.NextBytes(bytes);
            }
        }
    }
}
