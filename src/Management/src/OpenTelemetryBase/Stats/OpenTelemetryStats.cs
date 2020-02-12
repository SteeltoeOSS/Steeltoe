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
using System;

namespace Steeltoe.Management.OpenTelemetry.Stats
{
    public class OpenTelemetryStats : IStats
    {
        private static readonly Lazy<OpenTelemetryStats> AsSingleton = new Lazy<OpenTelemetryStats>(() => new OpenTelemetryStats());
        private Meter _meter = null;

        public static OpenTelemetryStats Instance => AsSingleton.Value;

        public Meter Meter
        {
            get
            {
                return _meter;
            }
        }

        //        private readonly IStatsComponent statsComponent = new StatsComponent();

        public OpenTelemetryStats(MetricProcessor processor=null)
        {
            _meter = MeterFactory.Create(processor).GetMeter("SteeltoeMeter");
        }

        //public IStatsRecorder StatsRecorder
        //{
        //    get
        //    {
        //        return statsComponent.StatsRecorder;
        //    }
        //}

        //public IViewManager ViewManager
        //{
        //    get
        //    {
        //        return statsComponent.ViewManager;
        //    }
        //}

        //public StatsCollectionState State
        //{
        //    get
        //    {
        //        return statsComponent.State;
        //    }

        //    set
        //    {
        //        ((StatsComponent)statsComponent).State = value;
        //    }
        //}
    }
}
