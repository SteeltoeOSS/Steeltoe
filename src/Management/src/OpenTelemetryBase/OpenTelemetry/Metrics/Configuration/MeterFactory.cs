#pragma warning disable SA1636 // File header copyright text should match

// <copyright file="MeterFactory.cs" company="OpenTelemetry Authors">
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

using Steeltoe.Management.OpenTelemetry.Metrics.Export;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.OpenTelemetry.Metrics.Configuration
{
    [Obsolete("OpenTelemetry Metrics API is not considered stable yet, see https://github.com/SteeltoeOSS/Steeltoe/issues/711 more information")]
    internal class MeterFactory : MeterFactoryBase
    {
        private readonly object _lck = new ();
        private readonly Dictionary<MeterRegistryKey, Meter> _meterRegistry = new ();
        private readonly MetricProcessor _metricProcessor;
        private Meter _defaultMeter;

        private MeterFactory(MetricProcessor metricProcessor)
        {
            if (metricProcessor == null)
            {
                _metricProcessor = new NoOpMetricProcessor();
            }
            else
            {
                _metricProcessor = metricProcessor;
            }

            _defaultMeter = new MeterSdk(string.Empty, _metricProcessor);
        }

        public static MeterFactory Create(MetricProcessor metricProcessor) => new(metricProcessor);

        public override Meter GetMeter(string name, string version = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                return _defaultMeter;
            }

            lock (_lck)
            {
                var key = new MeterRegistryKey(name, version);
                if (!_meterRegistry.TryGetValue(key, out var meter))
                {
                    meter = _defaultMeter = new MeterSdk(name, _metricProcessor);

                    _meterRegistry.Add(key, meter);
                }

                return meter;
            }
        }

        private readonly struct MeterRegistryKey
        {
            private readonly string _name;
            private readonly string _version;

            internal MeterRegistryKey(string name, string version)
            {
                _name = name;
                _version = version;
            }
        }
    }
}
