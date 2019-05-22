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

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public abstract class TracerBase : ITracer
    {
        private static readonly NoopTracer NoopTracerValue = new NoopTracer();

        internal static NoopTracer NoopTracer
        {
            get
            {
                return NoopTracerValue;
            }
        }

        public ISpan CurrentSpan
        {
            get
            {
                ISpan currentSpan = CurrentSpanUtils.CurrentSpan;
                return currentSpan != null ? currentSpan : BlankSpan.INSTANCE;
            }
        }

        public IScope WithSpan(ISpan span)
        {
            if (span == null)
            {
                throw new ArgumentNullException(nameof(span));
            }

            return CurrentSpanUtils.WithSpan(span, false);
        }

        // public final Runnable withSpan(Span span, Runnable runnable)
        // public final <C> Callable<C> withSpan(Span span, final Callable<C> callable)
        public ISpanBuilder SpanBuilder(string spanName)
        {
            return SpanBuilderWithExplicitParent(spanName, CurrentSpanUtils.CurrentSpan);
        }

        public abstract ISpanBuilder SpanBuilderWithExplicitParent(string spanName, ISpan parent = null);

        public abstract ISpanBuilder SpanBuilderWithRemoteParent(string spanName, ISpanContext remoteParentSpanContext = null);
    }
}
