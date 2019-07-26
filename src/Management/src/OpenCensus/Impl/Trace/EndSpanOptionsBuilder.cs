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

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public class EndSpanOptionsBuilder
    {
        private bool? sampleToLocalSpanStore;
        private Status status;

        internal EndSpanOptionsBuilder()
        {
        }

        public EndSpanOptionsBuilder SetSampleToLocalSpanStore(bool sampleToLocalSpanStore)
        {
            this.sampleToLocalSpanStore = sampleToLocalSpanStore;
            return this;
        }

        public EndSpanOptionsBuilder SetStatus(Status status)
        {
            this.status = status;
            return this;
        }

        public EndSpanOptions Build()
        {
            string missing = string.Empty;
            if (!this.sampleToLocalSpanStore.HasValue)
            {
                missing += " sampleToLocalSpanStore";
            }

            if (!string.IsNullOrEmpty(missing))
            {
                throw new ArgumentOutOfRangeException("Missing required properties:" + missing);
            }

            return new EndSpanOptions(
                this.sampleToLocalSpanStore.Value,
                this.status);
        }
    }
}
