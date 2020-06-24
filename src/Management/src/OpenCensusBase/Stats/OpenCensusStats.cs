// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenCensus.Stats;
using System;

namespace Steeltoe.Management.Census.Stats
{
    public class OpenCensusStats : IStats
    {
        private static readonly Lazy<OpenCensusStats> AsSingleton = new Lazy<OpenCensusStats>(() => new OpenCensusStats());

        public static OpenCensusStats Instance => AsSingleton.Value;

        private readonly IStatsComponent _statsComponent = new StatsComponent();

        public OpenCensusStats()
        {
        }

        public IStatsRecorder StatsRecorder
        {
            get
            {
                return _statsComponent.StatsRecorder;
            }
        }

        public IViewManager ViewManager
        {
            get
            {
                return _statsComponent.ViewManager;
            }
        }

        public StatsCollectionState State
        {
            get
            {
                return _statsComponent.State;
            }

            set
            {
                ((StatsComponent)_statsComponent).State = value;
            }
        }
    }
}
