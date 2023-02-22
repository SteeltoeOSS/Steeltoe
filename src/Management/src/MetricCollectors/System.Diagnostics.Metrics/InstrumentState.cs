// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security;

namespace System.Diagnostics.Metrics;

internal abstract class InstrumentState
{
    // This can be called concurrently with Collect()
    [SecuritySafeCritical]
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public abstract void Update(double measurement, ReadOnlySpan<KeyValuePair<string, object?>> labels);
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

    // This can be called concurrently with Update()
    public abstract void Collect(Instrument instrument, Action<LabeledAggregationStatistics> aggregationVisitFunc);
}

internal sealed class InstrumentState<TAggregator> : InstrumentState
    where TAggregator : Aggregator
{
    private AggregatorStore<TAggregator> _aggregatorStore;

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public InstrumentState(Func<TAggregator?> createAggregatorFunc)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        _aggregatorStore = new AggregatorStore<TAggregator>(createAggregatorFunc);
    }

    public override void Collect(Instrument instrument, Action<LabeledAggregationStatistics> aggregationVisitFunc)
    {
        _aggregatorStore.Collect(aggregationVisitFunc);
    }

    [SecuritySafeCritical]
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public override void Update(double measurement, ReadOnlySpan<KeyValuePair<string, object?>> labels)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        TAggregator? aggregator = _aggregatorStore.GetAggregator(labels);
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        aggregator?.Update(measurement);
    }
}
