// <copyright file="ISampledLatencyBucketBoundaries.cs" company="OpenCensus Authors">
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

namespace OpenCensus.Trace.Export
{
    /// <summary>
    /// Sampoled spans latency buckets for histograms calculations.
    /// </summary>
    public interface ISampledLatencyBucketBoundaries
    {
        /// <summary>
        /// Gets the lower latency boundary in nanoseconds.
        /// </summary>
        long LatencyLowerNs { get; }

        /// <summary>
        /// Gets the upper latency boundary in nanoseconds.
        /// </summary>
        long LatencyUpperNs { get; }
    }
}
