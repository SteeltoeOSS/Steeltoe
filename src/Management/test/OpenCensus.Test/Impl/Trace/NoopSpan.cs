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
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Census.Trace.Test
{
    [Obsolete]
    public class NoopSpan : SpanBase
    {
        public NoopSpan(ISpanContext context, SpanOptions options)
            : base(context, options)
        {
        }

        public override long EndNanoTime { get; }

        public override long LatencyNs { get; }

        public override bool IsSampleToLocalSpanStore { get; }

        public override Status Status { get; set; }

        public override string Name { get; }

        public override ISpanId ParentSpanId { get; }

        public override bool HasEnded => true;

        public override void AddAnnotation(string description, IDictionary<string, IAttributeValue> attributes)
        {
        }

        public override void AddAnnotation(IAnnotation annotation)
        {
        }

        public override void AddLink(ILink link)
        {
        }

        public override void AddMessageEvent(IMessageEvent messageEvent)
        {
        }

        public override void End(EndSpanOptions options)
        {
        }

        public override void PutAttributes(IDictionary<string, IAttributeValue> attributes)
        {
        }

        internal override ISpanData ToSpanData()
        {
            return null;
        }
    }
}
