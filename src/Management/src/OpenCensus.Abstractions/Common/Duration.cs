// <copyright file="Duration.cs" company="OpenCensus Authors">
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

namespace OpenCensus.Common
{
    public class Duration : IDuration
    {
        private const long MaxSeconds = 315576000000L;
        private const int MaxNanos = 999999999;
        private static readonly IDuration Zero = new Duration(0, 0);

        public Duration(long seconds, int nanos)
        {
            Seconds = seconds;
            Nanos = nanos;
        }

        public long Seconds { get; }

        public int Nanos { get; }

        public static IDuration Create(long seconds, int nanos)
        {
            if (seconds < -MaxSeconds || seconds > MaxSeconds)
            {
                return Zero;
            }

            if (nanos < -MaxNanos || nanos > MaxNanos)
            {
                return Zero;
            }

            if ((seconds < 0 && nanos > 0) || (seconds > 0 && nanos < 0))
            {
                return Zero;
            }

            return new Duration(seconds, nanos);
        }

        public int CompareTo(IDuration other)
        {
            var cmp = (Seconds < other.Seconds) ? -1 : ((Seconds > other.Seconds) ? 1 : 0);
            if (cmp != 0)
            {
                return cmp;
            }

            return (Nanos < other.Nanos) ? -1 : ((Nanos > other.Nanos) ? 1 : 0);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "Duration{"
                + "seconds=" + Seconds + ", "
                + "nanos=" + Nanos
                + "}";
        }

        /// <inheritdoc/>
        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is Duration that)
            {
                return (Seconds == that.Seconds)
                     && (Nanos == that.Nanos);
            }

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            long h = 1;
            h *= 1000003;
            h ^= (Seconds >> 32) ^ Seconds;
            h *= 1000003;
            h ^= Nanos;
            return (int)h;
        }
    }
}
