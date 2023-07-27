// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.MetricCollectors.Aggregations;

internal abstract class InstrumentState
{
    // This can be called concurrently with Collect()
    public abstract void Update(double measurement, ReadOnlySpan<KeyValuePair<string, object?>> labels);

    // This can be called concurrently with Update()
    public abstract void Collect(Action<LabeledAggregationStatistics> aggregationVisitFunc);
}

internal sealed class InstrumentState<TAggregator> : InstrumentState
    where TAggregator : Aggregator
{
    private AggregatorStore<TAggregator> _aggregatorStore;

    public InstrumentState(Func<TAggregator?> createAggregatorFunc)
    {
        _aggregatorStore = new AggregatorStore<TAggregator>(createAggregatorFunc);
    }

    public override void Collect(Action<LabeledAggregationStatistics> aggregationVisitFunc)
    {
        _aggregatorStore.Collect(aggregationVisitFunc);
    }

    public override void Update(double measurement, ReadOnlySpan<KeyValuePair<string, object?>> labels)
    {
        TAggregator? aggregator = _aggregatorStore.GetAggregator(labels);
        aggregator?.Update(measurement);
    }
}
