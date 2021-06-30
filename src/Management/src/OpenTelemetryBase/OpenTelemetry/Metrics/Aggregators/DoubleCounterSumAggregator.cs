#pragma warning disable SA1636 // File header copyright text should match

// <copyright file="DoubleCounterSumAggregator.cs" company="OpenTelemetry Authors">
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

using OpenTelemetry.Metrics.Export;
using System;
using System.Threading;

namespace OpenTelemetry.Metrics.Aggregators
{
    /// <summary>
    /// Basic aggregator which calculates a Sum from individual measurements.
    /// </summary>
    [Obsolete("OpenTelemetry Metrics API is not considered stable yet, see https://github.com/SteeltoeOSS/Steeltoe/issues/711 more information")]
    internal class DoubleCounterSumAggregator : Aggregator<double>
    {
        private double _sum;
        private double _checkPoint;

        public override void Checkpoint()
        {
            // checkpoints the current running sum into checkpoint, and starts counting again.
            _checkPoint = Interlocked.Exchange(ref _sum, 0.0);
        }

        public override MetricData<double> ToMetricData()
        {
            return new SumData<double>
            {
                Sum = _checkPoint,
                Timestamp = DateTime.UtcNow,
            };
        }

        public override AggregationType GetAggregationType() => AggregationType.DoubleSum;

        public override void Update(double value)
        {
            // Adds value to the running total in a thread safe manner.
            double initialTotal, computedTotal;
            do
            {
                initialTotal = _sum;
                computedTotal = initialTotal + value;
            }
            while (initialTotal != Interlocked.CompareExchange(ref _sum, computedTotal, initialTotal));
        }
    }
}
