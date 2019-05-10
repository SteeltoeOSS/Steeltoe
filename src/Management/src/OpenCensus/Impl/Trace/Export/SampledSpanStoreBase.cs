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

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public abstract class SampledSpanStoreBase : ISampledSpanStore
    {
        private static readonly ISampledSpanStore NOOP_SAMPLED_SPAN_STORE = new NoopSampledSpanStore();

        internal static ISampledSpanStore NoopSampledSpanStore
        {
            get
            {
                return NOOP_SAMPLED_SPAN_STORE;
            }
        }

        internal static ISampledSpanStore NewNoopSampledSpanStore
        {
            get
            {
                return new NoopSampledSpanStore();
            }
        }

        protected SampledSpanStoreBase()
        {
        }

        public abstract ISampledSpanStoreSummary Summary { get; }

        public abstract ISet<string> RegisteredSpanNamesForCollection { get; }

        public abstract void ConsiderForSampling(ISpan span);

        public abstract IList<ISpanData> GetErrorSampledSpans(ISampledSpanStoreErrorFilter filter);

        public abstract IList<ISpanData> GetLatencySampledSpans(ISampledSpanStoreLatencyFilter filter);

        public abstract void RegisterSpanNamesForCollection(IList<string> spanNames);

        public abstract void UnregisterSpanNamesForCollection(IList<string> spanNames);
    }
}
