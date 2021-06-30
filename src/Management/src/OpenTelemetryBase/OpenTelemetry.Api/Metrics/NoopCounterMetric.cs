#pragma warning disable SA1636 // File header copyright text should match

// <copyright file="NoOpCounterMetric.cs" company="OpenTelemetry Authors">
// Copyright 2018, OpenTelemetry Authors
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
#pragma warning restore SA1636 // File header copyright text should match

using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;

namespace OpenTelemetry.Metrics
{
    /// <summary>
    /// A no-op counter instrument.
    /// </summary>
    /// <typeparam name="T">The type of counter. Only long and double are supported now.</typeparam>
    [Obsolete("OpenTelemetry Metrics API is not considered stable yet, see https://github.com/SteeltoeOSS/Steeltoe/issues/711 more information")]
    internal sealed class NoOpCounterMetric<T> : CounterMetric<T>
        where T : struct
    {
        /// <summary>
        /// No op counter instance.
        /// </summary>
        public static readonly NoOpCounterMetric<T> Instance = new ();

        /// <inheritdoc/>
        public override void Add(in SpanContext context, T value, LabelSet labelset)
        {
        }

        /// <inheritdoc/>
        public override void Add(in SpanContext context, T value, IEnumerable<KeyValuePair<string, string>> labels)
        {
        }

        /// <inheritdoc/>
        public override BoundCounterMetric<T> Bind(LabelSet labelset) => NoOpBoundCounterMetric<T>.Instance;

        /// <inheritdoc/>
        public override BoundCounterMetric<T> Bind(IEnumerable<KeyValuePair<string, string>> labels) => NoOpBoundCounterMetric<T>.Instance;
    }
}
