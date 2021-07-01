#pragma warning disable SA1636 // File header copyright text should match

// <copyright file="NoOpMeter.cs" company="OpenTelemetry Authors">
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

using System;
using System.Collections.Generic;

namespace Steeltoe.Management.OpenTelemetry.Metrics
{
    [Obsolete("OpenTelemetry Metrics API is not considered stable yet, see https://github.com/SteeltoeOSS/Steeltoe/issues/711 more information")]
    internal sealed class NoOpMeter : Meter
    {
        public NoOpMeter()
        {
        }

        public override CounterMetric<double> CreateDoubleCounter(string name, bool monotonic = true) => NoOpCounterMetric<double>.Instance;

        public override MeasureMetric<double> CreateDoubleMeasure(string name, bool absolute = true) => NoOpMeasureMetric<double>.Instance;

        public override DoubleObserverMetric CreateDoubleObserver(string name, Action<DoubleObserverMetric> callback, bool absolute = true) => NoOpDoubleObserverMetric.Instance;

        public override CounterMetric<long> CreateInt64Counter(string name, bool monotonic = true) => NoOpCounterMetric<long>.Instance;

        public override MeasureMetric<long> CreateInt64Measure(string name, bool absolute = true) => NoOpMeasureMetric<long>.Instance;

        public override Int64ObserverMetric CreateInt64Observer(string name, Action<Int64ObserverMetric> callback, bool absolute = true) => NoOpInt64ObserverMetric.Instance;

        public override LabelSet GetLabelSet(IEnumerable<KeyValuePair<string, string>> labels) => LabelSet.BlankLabelSet;
    }
}
