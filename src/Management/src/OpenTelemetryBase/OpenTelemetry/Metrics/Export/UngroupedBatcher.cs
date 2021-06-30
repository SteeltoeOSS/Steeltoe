#pragma warning disable SA1636 // File header copyright text should match

// <copyright file="UngroupedBatcher.cs" company="OpenTelemetry Authors">
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

using OpenTelemetry.Metrics.Aggregators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace OpenTelemetry.Metrics.Export
{
    /// <summary>
    /// Batcher which retains all dimensions/labels.
    /// </summary>
    [Obsolete("OpenTelemetry Metrics API is not considered stable yet, see https://github.com/SteeltoeOSS/Steeltoe/issues/711 more information")]
    internal class UngroupedBatcher : MetricProcessor
    {
        private readonly MetricExporter _exporter;
        private readonly Task _worker;
        private readonly TimeSpan _aggregationInterval;
        private CancellationTokenSource _cts;
        private List<Metric<long>> _longMetrics;
        private List<Metric<double>> _doubleMetrics;

        /// <summary>
        /// Initializes a new instance of the <see cref="UngroupedBatcher"/> class.
        /// </summary>
        /// <param name="exporter">Metric exporter instance.</param>
        /// <param name="aggregationInterval">Interval at which metrics are pushed to Exporter.</param>
        public UngroupedBatcher(MetricExporter exporter, TimeSpan aggregationInterval)
        {
            _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));

            // TODO make this thread safe.
            _longMetrics = new List<Metric<long>>();
            _doubleMetrics = new List<Metric<double>>();
            _aggregationInterval = aggregationInterval;
            _cts = new CancellationTokenSource();
            _worker = Task.Factory.StartNew(
                s => Worker((CancellationToken)s), _cts.Token).ContinueWith((task) => Console.WriteLine("error"), TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UngroupedBatcher"/> class.
        /// </summary>
        /// <param name="exporter">Metric exporter instance.</param>
        public UngroupedBatcher(MetricExporter exporter)
            : this(exporter, TimeSpan.FromSeconds(5))
        {
        }

        public override void Process(string meterName, string metricName, LabelSet labelSet, Aggregator<long> aggregator)
        {
            var metric = new Metric<long>(meterName, metricName, meterName + metricName, labelSet.Labels, aggregator.GetAggregationType());
            metric.Data = aggregator.ToMetricData();
            _longMetrics.Add(metric);
        }

        public override void Process(string meterName, string metricName, LabelSet labelSet, Aggregator<double> aggregator)
        {
            var metric = new Metric<double>(meterName, metricName, meterName + metricName, labelSet.Labels, aggregator.GetAggregationType());
            metric.Data = aggregator.ToMetricData();
            _doubleMetrics.Add(metric);
        }

        private async Task Worker(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(_aggregationInterval, cancellationToken).ConfigureAwait(false);
                while (!cancellationToken.IsCancellationRequested)
                {
                    var sw = Stopwatch.StartNew();

                    if (_longMetrics.Count > 0)
                    {
                        var metricToExport = _longMetrics;
                        _longMetrics = new List<Metric<long>>();
                        await _exporter.ExportAsync<long>(metricToExport, cancellationToken);
                    }

                    if (_doubleMetrics.Count > 0)
                    {
                        var metricToExport = _doubleMetrics;
                        _doubleMetrics = new List<Metric<double>>();
                        await _exporter.ExportAsync<double>(metricToExport, cancellationToken);
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var remainingWait = _aggregationInterval - sw.Elapsed;
                    if (remainingWait > TimeSpan.Zero)
                    {
                        await Task.Delay(remainingWait, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = ex.Message;
            }
        }
    }
}
