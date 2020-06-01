// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenCensus.Stats;

namespace Steeltoe.Management.Census.Stats
{
    public interface IStats
    {
        IStatsRecorder StatsRecorder { get; }

        IViewManager ViewManager { get; }

        StatsCollectionState State { get; set; }
    }
}
