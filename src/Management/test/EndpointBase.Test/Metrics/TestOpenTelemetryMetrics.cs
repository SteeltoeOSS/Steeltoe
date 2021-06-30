// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry.Metrics;
using Steeltoe.Management.OpenTelemetry.Metrics.Exporter;
using Steeltoe.Management.OpenTelemetry.Metrics.Factory;
using Steeltoe.Management.OpenTelemetry.Metrics.Processor;
using Steeltoe.Management.OpenTelemetry.Stats;
using System;

namespace Steeltoe.Management.EndpointBase.Test.Metrics
{
    [Obsolete]
    public class TestOpenTelemetryMetrics : IStats
    {
        public TestOpenTelemetryMetrics()
        {
            Exporter = new SteeltoeExporter();
            Processor = new SteeltoeProcessor(Exporter);
            Factory = AutoCollectingMeterFactory.Create(Processor);
            Meter = Factory.GetMeter("Test");
        }

        public Meter Meter { get; set; }

        public AutoCollectingMeterFactory Factory { get; set; }

        public SteeltoeExporter Exporter { get; set; }

        public SteeltoeProcessor Processor { get; set; }
    }
}
