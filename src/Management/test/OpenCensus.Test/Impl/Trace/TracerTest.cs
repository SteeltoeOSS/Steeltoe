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
using Steeltoe.Management.Census.Trace.Internal;
using System;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    [Obsolete]
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
