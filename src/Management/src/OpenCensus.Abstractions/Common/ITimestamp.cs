// <copyright file="ITimestamp.cs" company="OpenCensus Authors">
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
    using System;

    /// <summary>
    /// Timestamp with the nanoseconds precision.
    /// </summary>
    public interface ITimestamp : IComparable<ITimestamp>
    {
        /// <summary>
        /// Gets the number of seconds since the Unix Epoch represented by this timestamp.
        /// </summary>
        long Seconds { get; }

        /// <summary>
        /// Gets the the number of nanoseconds after the number of seconds since the Unix Epoch represented
        /// by this timestamp.
        /// </summary>
        int Nanos { get; }

        /// <summary>
        /// Adds nanosToAdd nanosecond to the current timestamp.
        /// </summary>
        /// <param name="nanosToAdd">Number of nanoseconds to add.</param>
        /// <returns>Returns the timstemp with added nanoseconds.</returns>
        ITimestamp AddNanos(long nanosToAdd);

        /// <summary>
        /// Adds duration to the timestamp.
        /// </summary>
        /// <param name="duration">Duration to add to the timestamp.</param>
        /// <returns>Returns the timestamp with added duration.</returns>
        ITimestamp AddDuration(IDuration duration);

        /// <summary>
        /// Substructs timestamp from the current timestamp. Typically to calculate duration.
        /// </summary>
        /// <param name="timestamp">Timestamp to substruct.</param>
        /// <returns>Returns the timestamp with the substructed duration.</returns>
        IDuration SubtractTimestamp(ITimestamp timestamp);
    }
}
