// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using HdrHistogram;
using System.Reactive.Linq;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;

public abstract class RollingDistributionStreamBase
{
    protected static Func<LongHistogram, LongHistogram, LongHistogram> DistributionAggregator { get; } = (initialDistribution, distributionToAdd) =>
    {
        initialDistribution.Add(distributionToAdd);
        return initialDistribution;
    };

    protected static Func<IObservable<LongHistogram>, IObservable<LongHistogram>> ReduceWindowToSingleDistribution { get; } = window =>
    {
        var result = window.Aggregate((arg1, arg2) => DistributionAggregator(arg1, arg2)).Select(n => n);
        return result;
    };

    protected static Func<LongHistogram, CachedValuesHistogram> CacheHistogramValues { get; } = CachedValuesHistogram.BackedBy;

    protected static Func<IObservable<CachedValuesHistogram>, IObservable<IList<CachedValuesHistogram>>> ConvertToList { get; } = windowOf2 => windowOf2.ToList();
}
