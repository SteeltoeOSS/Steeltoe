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

using OpenTelemetry.Metrics;
using OpenTelemetry.Metrics.Configuration;
using OpenTelemetry.Metrics.Export;
using Steeltoe.Management.OpenTelemetry.Metrics.Processor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Management.OpenTelemetry.Metrics.Factory
{
    public class AutoCollectingMeterFactory : MeterFactoryBase
    {
        private readonly HashSet<(string, string)> _meterRegistryKeySet = new HashSet<(string, string)>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Task _worker;
        private readonly MetricProcessor _processor;

        private readonly MeterFactory _meterFactory;
        private TimeSpan _collectionInterval;

        public AutoCollectingMeterFactory(MetricProcessor processor, TimeSpan timeSpan)
            : base()
        {
            _meterFactory = MeterFactory.Create(processor);

            _processor = processor;
            _collectionInterval = timeSpan;

            if (processor != null && timeSpan < TimeSpan.MaxValue)
            {
                _worker = Task.Factory.StartNew(
                    s => Worker((CancellationToken)s), _cts.Token).ContinueWith((task) => Console.WriteLine("error"), TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        public static AutoCollectingMeterFactory Create(MetricProcessor processor)
        {
            return new AutoCollectingMeterFactory(processor, TimeSpan.MaxValue);
        }

        public override Meter GetMeter(string name, string version = null)
        {
            if (!_meterRegistryKeySet.Contains((name, version)))
            {
                _meterRegistryKeySet.Add((name, version));
            }

            return _meterFactory.GetMeter(name, version);
        }

        internal void CollectAllMetrics()
        {
            foreach (var (name, version) in _meterRegistryKeySet)
            {
                var meter = _meterFactory?.GetMeter(name, version);
                (meter as MeterSdk)?.Collect();
            }
        }

        private async Task Worker(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(_collectionInterval, cancellationToken).ConfigureAwait(false);
                while (!cancellationToken.IsCancellationRequested)
                {
                    var sw = Stopwatch.StartNew();

                    CollectAllMetrics();
                    (_processor as SteeltoeProcessor)?.ExportMetrics();

                    // (_processor as SteeltoeProcessor)?.Clear();
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var remainingWait = _collectionInterval - sw.Elapsed;
                    if (remainingWait > TimeSpan.Zero)
                    {
                        await Task.Delay(remainingWait, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                var s = ex.Message;
            }
        }
    }
}
