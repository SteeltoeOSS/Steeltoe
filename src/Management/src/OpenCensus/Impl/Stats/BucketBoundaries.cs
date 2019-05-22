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

using Steeltoe.Management.Census.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class BucketBoundaries : IBucketBoundaries
    {
        public IList<double> Boundaries { get; }

        BucketBoundaries(IList<double> boundaries)
        {
            this.Boundaries = boundaries;
        }

        public static IBucketBoundaries Create(IList<double> bucketBoundaries)
        {
            if (bucketBoundaries == null)
            {
                throw new ArgumentNullException(nameof(bucketBoundaries));
            }

            List<double> bucketBoundariesCopy = new List<double>(bucketBoundaries);

            if (bucketBoundariesCopy.Count > 1)
            {
                double lower = bucketBoundariesCopy[0];
                for (int i = 1; i < bucketBoundariesCopy.Count; i++)
                {
                    double next = bucketBoundariesCopy[i];
                    if (!(lower < next))
                    {
                        throw new ArgumentOutOfRangeException("Bucket boundaries not sorted.");
                    }

                    lower = next;
                }
            }

            return new BucketBoundaries(bucketBoundariesCopy.AsReadOnly());
        }

        public override string ToString()
        {
            return "BucketBoundaries{"
                + "boundaries=" + Collections.ToString(Boundaries)
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is BucketBoundaries)
            {
                BucketBoundaries that = (BucketBoundaries)o;
                return this.Boundaries.SequenceEqual(that.Boundaries);
            }

            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.Boundaries.GetHashCode();
            return h;
        }
    }
}
