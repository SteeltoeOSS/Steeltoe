using OpenTelemetry.Metrics;
using OpenTelemetry.Metrics.Export;
using Steeltoe.Management.OpenTelemetry.Metrics.Exporter;
using Steeltoe.Management.OpenTelemetry.Metrics.Factory;
using Steeltoe.Management.OpenTelemetry.Metrics.Processor;
using Steeltoe.Management.OpenTelemetry.Stats;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.EndpointBase.Test.Metrics
{
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
