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

using Steeltoe.Management.Census.Trace.Export;
using Steeltoe.Management.Census.Utils;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public abstract class SpanBase : ISpan, IElement<SpanBase>
    {
        private static readonly IDictionary<string, IAttributeValue> EMPTY_ATTRIBUTES = new Dictionary<string, IAttributeValue>();

        public virtual ISpanContext Context { get; }

        public virtual SpanOptions Options { get; }

        public abstract Status Status { get; set; }

        public abstract string Name { get; }

        public SpanBase Next { get; set; }

        public SpanBase Previous { get; set; }

        public abstract long EndNanoTime { get; }

        public abstract long LatencyNs { get; }

        public abstract bool IsSampleToLocalSpanStore { get; }

        public abstract ISpanId ParentSpanId { get; }

        internal SpanBase()
        {
        }

        protected SpanBase(ISpanContext context, SpanOptions options = SpanOptions.NONE)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.TraceOptions.IsSampled && !options.HasFlag(SpanOptions.RECORD_EVENTS))
            {
                throw new ArgumentOutOfRangeException("Span is sampled, but does not have RECORD_EVENTS set.");
            }

            Context = context;
            Options = options;
        }

        public virtual void PutAttribute(string key, IAttributeValue value)
        {
            PutAttributes(new Dictionary<string, IAttributeValue>() { { key, value } });
        }

        public abstract void PutAttributes(IDictionary<string, IAttributeValue> attributes);

        public void AddAnnotation(string description)
        {
            AddAnnotation(description, EMPTY_ATTRIBUTES);
        }

        public abstract void AddAnnotation(string description, IDictionary<string, IAttributeValue> attributes);

        public abstract void AddAnnotation(IAnnotation annotation);

        public abstract void AddMessageEvent(IMessageEvent messageEvent);

        public abstract void AddLink(ILink link);

        public abstract void End(EndSpanOptions options);

        internal abstract ISpanData ToSpanData();

        public void End()
        {
            End(EndSpanOptions.DEFAULT);
        }

        public abstract bool HasEnded { get; }

        public override string ToString()
        {
            return "Span[" +
                Name +
                "]";
        }
    }
}
