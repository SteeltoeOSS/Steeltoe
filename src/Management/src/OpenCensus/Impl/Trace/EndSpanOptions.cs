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
    public class EndSpanOptions
    {
        public static readonly EndSpanOptions DEFAULT = new EndSpanOptions(false);

        public bool SampleToLocalSpanStore { get; }

        public Status Status { get; }

        internal EndSpanOptions()
        {
        }

        internal EndSpanOptions(bool sampleToLocalSpanStore, Status status = null)
        {
            SampleToLocalSpanStore = sampleToLocalSpanStore;
            Status = status;
        }

        public static EndSpanOptionsBuilder Builder()
        {
            return new EndSpanOptionsBuilder().SetSampleToLocalSpanStore(false);
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (obj is EndSpanOptions)
            {
                EndSpanOptions that = (EndSpanOptions)obj;
                return (this.SampleToLocalSpanStore == that.SampleToLocalSpanStore)
                     && ((this.Status == null) ? (that.Status == null) : this.Status.Equals(that.Status));
            }

            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.SampleToLocalSpanStore ? 1231 : 1237;
            h *= 1000003;
            h ^= (Status == null) ? 0 : this.Status.GetHashCode();
            return h;
        }

        public override string ToString()
        {
            return "EndSpanOptions{"
                + "sampleToLocalSpanStore=" + SampleToLocalSpanStore + ", "
                + "status=" + Status
                + "}";
        }
    }
}
