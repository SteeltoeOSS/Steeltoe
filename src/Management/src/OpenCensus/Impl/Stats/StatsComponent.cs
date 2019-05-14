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

using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Internal;
using System;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    public class StatsComponent : StatsComponentBase
    {
        // The StatsCollectionState shared between the StatsComponent, StatsRecorder and ViewManager.
        private readonly CurrentStatsState state = new CurrentStatsState();

        private readonly IViewManager viewManager;
        private readonly IStatsRecorder statsRecorder;

        public StatsComponent()
            : this(new SimpleEventQueue(), DateTimeOffsetClock.INSTANCE)
        {
        }

        public StatsComponent(IEventQueue queue, IClock clock)
        {
            StatsManager statsManager = new StatsManager(queue, clock, state);
            this.viewManager = new ViewManager(statsManager);
            this.statsRecorder = new StatsRecorder(statsManager);
        }

        public override IViewManager ViewManager
        {
            get { return viewManager; }
        }

        public override IStatsRecorder StatsRecorder
        {
            get { return statsRecorder; }
        }

        public override StatsCollectionState State
        {
            get
            {
                return state.Value;
            }

            set
            {
                ViewManager manager = viewManager as ViewManager;
                if (manager == null)
                {
                    return;
                }

                var result = state.Set(value);
                if (result)
                {
                    if (value == StatsCollectionState.DISABLED)
                    {
                        manager.ClearStats();
                    }
                    else
                    {
                        manager.ResumeStatsCollection();
                    }
                }
            }
        }
    }
}
