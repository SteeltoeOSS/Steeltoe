// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry.Metrics;
using OpenTelemetry.Metrics.Configuration;
using OpenTelemetry.Metrics.Export;
using Steeltoe.Management.OpenTelemetry.Metrics.Factory;
using System;

namespace Steeltoe.Management.OpenTelemetry.Stats
{
    public class OpenTelemetryMetrics : IStats
    {
        private static readonly Lazy<OpenTelemetryMetrics> AsSingleton = new Lazy<OpenTelemetryMetrics>(() => new OpenTelemetryMetrics());
        private Meter _meter = null;

        public static OpenTelemetryMetrics Instance => AsSingleton.Value;

        public Meter Meter
        {
            get
            {
                return _meter;
            }
        }

        public OpenTelemetryMetrics(MetricProcessor processor = null)
        {
            _meter = MeterFactory.Create(processor).GetMeter("Steeltoe");
        }

        public OpenTelemetryMetrics(MetricProcessor processor, TimeSpan timeSpan)
        {
            var factory = new AutoCollectingMeterFactory(processor, timeSpan);
            _meter = factory.GetMeter("Steeltoe");
        }
    }
}
