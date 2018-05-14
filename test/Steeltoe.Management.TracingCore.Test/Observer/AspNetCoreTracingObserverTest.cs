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

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Census.Trace;
using Steeltoe.Management.Tracing.Test;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Tracing.Observer.Test
{
    public class AspNetCoreTracingObserverTest : TestBase
    {
        [Fact]
        public void EndSpanIfNeeded()
        {
            var opts = GetOptions();
            OpenCensusTracing tracing = new OpenCensusTracing(opts, null);

            // Should be Ignored
            AspNetCoreTracingObserver.EndSpanIfNeeded(null, null, false);

            var span = tracing.Tracer.SpanBuilder("MySpan").StartSpan() as Span;

            AspNetCoreTracingObserver.SpanContext newContext = new AspNetCoreTracingObserver.SpanContext(span, null);

            // Should be Ignored
            AspNetCoreTracingObserver.EndSpanIfNeeded(null, newContext, true);
            Assert.False(span.HasEnded);

            AspNetCoreTracingObserver.SpanContext prevContext = new AspNetCoreTracingObserver.SpanContext(null, null);

            // Should be Ignored
            AspNetCoreTracingObserver.EndSpanIfNeeded(prevContext, newContext, true);
            Assert.False(span.HasEnded);

            prevContext = new AspNetCoreTracingObserver.SpanContext(span, null);

            // Should End
            AspNetCoreTracingObserver.EndSpanIfNeeded(prevContext, null, true);
            Assert.True(span.HasEnded);
        }
    }
}
