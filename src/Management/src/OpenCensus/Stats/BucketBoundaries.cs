// <copyright file="BucketBoundaries.cs" company="OpenCensus Authors">
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

namespace OpenCensus.Stats
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using OpenCensus.Utils;

    public sealed class BucketBoundaries : IBucketBoundaries
    {
        private BucketBoundaries(IList<double> boundaries)
        {
            Boundaries = boundaries;
        }

        public IList<double> Boundaries { get; }

        public static IBucketBoundaries Create(IList<double> bucketBoundaries)
        {
            if (bucketBoundaries == null)
            {
                throw new ArgumentNullException(nameof(bucketBoundaries));
            }

            var bucketBoundariesCopy = new List<double>(bucketBoundaries);

            if (bucketBoundariesCopy.Count > 1)
            {
                var lower = bucketBoundariesCopy[0];
                for (var i = 1; i < bucketBoundariesCopy.Count; i++)
                {
                    var next = bucketBoundariesCopy[i];
                    if (!(lower < next))
                    {
                        throw new ArgumentOutOfRangeException("Bucket boundaries not sorted.");
                    }

                    lower = next;
                }
            }

            return new BucketBoundaries(bucketBoundariesCopy.AsReadOnly());
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "BucketBoundaries{"
                + "boundaries=" + Collections.ToString(Boundaries)
                + "}";
        }

        /// <inheritdoc/>
        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is BucketBoundaries that)
            {
                return Boundaries.SequenceEqual(that.Boundaries);
            }

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var h = 1;
            h *= 1000003;
            h ^= Boundaries.GetHashCode();
            return h;
        }
    }
}
