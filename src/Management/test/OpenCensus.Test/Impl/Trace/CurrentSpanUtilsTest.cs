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
using Steeltoe.Management.Census.Trace.Internal;
using System;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    [Obsolete]
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
