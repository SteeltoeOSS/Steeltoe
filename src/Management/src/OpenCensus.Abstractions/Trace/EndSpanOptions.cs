// <copyright file="EndSpanOptions.cs" company="OpenCensus Authors">
// Copyright 2018, OpenCensus Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenCensus.Trace
{
    /// <summary>
    /// End span options.
    /// </summary>
    public class EndSpanOptions
    {
        /// <summary>
        /// Default span completion options.
        /// </summary>
        public static readonly EndSpanOptions Default = new EndSpanOptions(false);

        internal EndSpanOptions()
        {
        }

        internal EndSpanOptions(bool sampleToLocalSpanStore, Status status = null)
        {
            SampleToLocalSpanStore = sampleToLocalSpanStore;
            Status = status;
        }

        /// <summary>
        /// Gets a value indicating whether span needs to be stored into local store.
        /// </summary>
        public bool SampleToLocalSpanStore { get; }

        /// <summary>
        /// Gets the span status.
        /// </summary>
        public Status Status { get; }

        /// <summary>
        /// Gets the span builder.
        /// </summary>
        /// <returns>Returns builder to build span options.</returns>
        public static EndSpanOptionsBuilder Builder()
        {
            return new EndSpanOptionsBuilder().SetSampleToLocalSpanStore(false);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (obj is EndSpanOptions that)
            {
                return (SampleToLocalSpanStore == that.SampleToLocalSpanStore)
                     && ((Status == null) ? (that.Status == null) : Status.Equals(that.Status));
            }

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var h = 1;
            h *= 1000003;
            h ^= SampleToLocalSpanStore ? 1231 : 1237;
            h *= 1000003;
            h ^= (Status == null) ? 0 : Status.GetHashCode();
            return h;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "EndSpanOptions{"
                + "sampleToLocalSpanStore=" + SampleToLocalSpanStore + ", "
                + "status=" + Status
                + "}";
        }
    }
}
