// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry.Metrics;
using OpenTelemetry.Metrics.Configuration;
using OpenTelemetry.Metrics.Export;
using Steeltoe.Management.OpenTelemetry.Metrics.Processor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Management.OpenTelemetry.Metrics.Factory
{
    [Obsolete("OpenTelemetry Metrics API is not considered stable yet, see https://github.com/SteeltoeOSS/Steeltoe/issues/711 more information")]
    public class AutoCollectingMeterFactory : MeterFactoryBase
    {
        private readonly HashSet<(string, string)> _meterRegistryKeySet = new ();
        private readonly CancellationTokenSource _cts = new ();
        private readonly Task _worker;
        private readonly MetricProcessor _processor;

        private MeterFactory _meterFactory;
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

        public static AutoCollectingMeterFactory Create(MetricProcessor processor) => new AutoCollectingMeterFactory(processor, TimeSpan.MaxValue);

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
                _ = ex.Message;
            }
        }
    }
}
