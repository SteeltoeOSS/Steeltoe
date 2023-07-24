// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace Steeltoe.Management.MetricCollectors.Aggregations;

internal sealed class FixedSizeLabelNameDictionary<TStringSequence, TObjectSequence, TAggregator>
    : ConcurrentDictionary<TStringSequence, ConcurrentDictionary<TObjectSequence, TAggregator>>
    where TAggregator : Aggregator
    where TStringSequence : IStringSequence, IEquatable<TStringSequence>
    where TObjectSequence : IObjectSequence, IEquatable<TObjectSequence>
{
    public void Collect(Action<LabeledAggregationStatistics> visitFunc)
    {
        foreach (KeyValuePair<TStringSequence, ConcurrentDictionary<TObjectSequence, TAggregator>> kvName in this)
        {
            Span<string> indexedNames = kvName.Key.AsSpan();

            foreach (KeyValuePair<TObjectSequence, TAggregator> kvValue in kvName.Value)
            {
                Span<object?> indexedValues = kvValue.Key.AsSpan();
                var labels = new KeyValuePair<string, string>[indexedNames.Length];

                for (int i = 0; i < labels.Length; i++)
                {
                    labels[i] = new KeyValuePair<string, string>(indexedNames[i], indexedValues[i]?.ToString() ?? string.Empty);
                }

                IAggregationStatistics stats = kvValue.Value.Collect();
                visitFunc(new LabeledAggregationStatistics(stats, labels));
            }
        }
    }

    public ConcurrentDictionary<TObjectSequence, TAggregator> GetValuesDictionary(in TStringSequence names)
    {
        return GetOrAdd(names, _ => new ConcurrentDictionary<TObjectSequence, TAggregator>());
    }
}
