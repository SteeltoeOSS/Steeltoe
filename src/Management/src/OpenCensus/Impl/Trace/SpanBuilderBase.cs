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

using Steeltoe.Management.Census.Common;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public abstract class SpanBuilderBase : ISpanBuilder
    {
        public abstract ISpanBuilder SetSampler(ISampler sampler);

        public abstract ISpanBuilder SetParentLinks(IList<ISpan> parentLinks);

        public abstract ISpanBuilder SetRecordEvents(bool recordEvents);

        public abstract ISpan StartSpan();

        public IScope StartScopedSpan()
        {
            return CurrentSpanUtils.WithSpan(StartSpan(), true);
        }

        // public void StartSpanAndRun(Runnable runnable)
        // {
        //    Span span = startSpan();
        //    CurrentSpanUtils.withSpan(span, /* endSpan= */ true, runnable).run();
        // }
        // public final<V> V startSpanAndCall(Callable<V> callable) throws Exception
        // {
        //    final Span span = startSpan();
        //    return CurrentSpanUtils.withSpan(span, /* endSpan= */ true, callable).call();
        // }
    }
}
