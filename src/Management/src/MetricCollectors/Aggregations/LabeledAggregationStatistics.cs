// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;

namespace Steeltoe.Management.MetricCollectors.Aggregations;

public sealed class LabeledAggregationStatistics
{
    public IList<KeyValuePair<string, string>> Labels { get; }
    public IAggregationStatistics AggregationStatistics { get; }

    public LabeledAggregationStatistics(IAggregationStatistics stats, params KeyValuePair<string, string>[] labels)
    {
        ArgumentGuard.NotNull(stats);
        ArgumentGuard.NotNull(labels);

        AggregationStatistics = stats;
        Labels = labels;
    }
}