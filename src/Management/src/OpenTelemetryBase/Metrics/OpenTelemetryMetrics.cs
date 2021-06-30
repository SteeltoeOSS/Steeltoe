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
    [Obsolete("Steeltoe uses the OpenTelemetry Metrics API, which is not considered stable yet, see https://github.com/SteeltoeOSS/Steeltoe/issues/711 more information")]
    public class OpenTelemetryMetrics : IStats
    {
        private static readonly Lazy<OpenTelemetryMetrics> _asSingleton = new (() => new OpenTelemetryMetrics());

        public static OpenTelemetryMetrics Instance => _asSingleton.Value;

        public Meter Meter { get; }

        public OpenTelemetryMetrics(MetricProcessor processor = null)
        {
            Meter = MeterFactory.Create(processor).GetMeter("Steeltoe");
        }

        public OpenTelemetryMetrics(MetricProcessor processor, TimeSpan timeSpan)
        {
            var factory = new AutoCollectingMeterFactory(processor, timeSpan);
            Meter = factory.GetMeter("Steeltoe");
        }
    }
}
