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

using HdrHistogram;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer
{
    public abstract class RollingDistributionStreamBase
    {
        protected RollingDistributionStreamBase()
        {
        }

        protected static Func<LongHistogram, LongHistogram, LongHistogram> DistributionAggregator { get; } = (initialDistribution, distributionToAdd) =>
        {
            initialDistribution.Add(distributionToAdd);
            return initialDistribution;
        };

        protected static Func<IObservable<LongHistogram>, IObservable<LongHistogram>> ReduceWindowToSingleDistribution { get; } = (window) =>
        {
            var result = window.Aggregate((arg1, arg2) => DistributionAggregator(arg1, arg2)).Select(n => n);
            return result;
        };

        protected static Func<LongHistogram, CachedValuesHistogram> CacheHistogramValues { get; } = (histogram) =>
        {
            return CachedValuesHistogram.BackedBy(histogram);
        };

        protected static Func<IObservable<CachedValuesHistogram>, IObservable<IList<CachedValuesHistogram>>> ConvertToList { get; } = (windowOf2) =>
        {
            return windowOf2.ToList();
        };
    }
}
