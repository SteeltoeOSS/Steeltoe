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
