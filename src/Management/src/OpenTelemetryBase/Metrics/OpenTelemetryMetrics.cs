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
