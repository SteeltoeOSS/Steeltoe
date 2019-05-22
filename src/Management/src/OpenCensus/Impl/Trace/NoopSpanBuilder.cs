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

using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public class NoopSpanBuilder : SpanBuilderBase
    {
        internal static ISpanBuilder CreateWithParent(string spanName, ISpan parent = null)
        {
            return new NoopSpanBuilder(spanName);
        }

        internal static ISpanBuilder CreateWithRemoteParent(string spanName, ISpanContext remoteParentSpanContext = null)
        {
            return new NoopSpanBuilder(spanName);
        }

        public override ISpan StartSpan()
        {
            return BlankSpan.INSTANCE;
        }

        public override ISpanBuilder SetSampler(ISampler sampler)
        {
            return this;
        }

        public override ISpanBuilder SetParentLinks(IList<ISpan> parentLinks)
        {
            return this;
        }

        public override ISpanBuilder SetRecordEvents(bool recordEvents)
        {
            return this;
        }

        private NoopSpanBuilder(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
        }
    }
}
