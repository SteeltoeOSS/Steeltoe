// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Steeltoe.Management.MetricCollectors.Aggregations;

internal sealed class MultiSizeLabelNameDictionary<TAggregator>
    where TAggregator : Aggregator
{
    private TAggregator? _noLabelAggregator;
    private FixedSizeLabelNameDictionary<StringSequence1, ObjectSequence1, TAggregator>? _label1;
    private FixedSizeLabelNameDictionary<StringSequence2, ObjectSequence2, TAggregator>? _label2;
    private FixedSizeLabelNameDictionary<StringSequence3, ObjectSequence3, TAggregator>? _label3;
    private FixedSizeLabelNameDictionary<StringSequenceMany, ObjectSequenceMany, TAggregator>? _labelMany;

    public MultiSizeLabelNameDictionary(object initialLabelNameDict)
    {
        _noLabelAggregator = null;
        _label1 = null;
        _label2 = null;
        _label3 = null;
        _labelMany = null;

        switch (initialLabelNameDict)
        {
            case TAggregator val0:
                _noLabelAggregator = val0;
                break;

            case FixedSizeLabelNameDictionary<StringSequence1, ObjectSequence1, TAggregator> val1:
                _label1 = val1;
                break;

            case FixedSizeLabelNameDictionary<StringSequence2, ObjectSequence2, TAggregator> val2:
                _label2 = val2;
                break;

            case FixedSizeLabelNameDictionary<StringSequence3, ObjectSequence3, TAggregator> val3:
                _label3 = val3;
                break;

            case FixedSizeLabelNameDictionary<StringSequenceMany, ObjectSequenceMany, TAggregator> valMany:
                _labelMany = valMany;
                break;
        }
    }

    public TAggregator? GetNoLabelAggregator(Func<TAggregator?> createFunc)
    {
        if (_noLabelAggregator == null)
        {
            TAggregator? aggregator = createFunc();

            if (aggregator != null)
            {
                Interlocked.CompareExchange(ref _noLabelAggregator, aggregator, null);
            }
        }

        return _noLabelAggregator;
    }

    public FixedSizeLabelNameDictionary<TStringSequence, TObjectSequence, TAggregator> GetFixedSizeLabelNameDictionary<TStringSequence, TObjectSequence>()
        where TStringSequence : IStringSequence, IEquatable<TStringSequence>
        where TObjectSequence : IObjectSequence, IEquatable<TObjectSequence>
    {
        TStringSequence? seq = default;

        switch (seq)
        {
            case StringSequence1:
                if (_label1 == null)
                {
                    Interlocked.CompareExchange(ref _label1, new FixedSizeLabelNameDictionary<StringSequence1, ObjectSequence1, TAggregator>(), null);
                }

                return (FixedSizeLabelNameDictionary<TStringSequence, TObjectSequence, TAggregator>)(object)_label1;

            case StringSequence2:
                if (_label2 == null)
                {
                    Interlocked.CompareExchange(ref _label2, new FixedSizeLabelNameDictionary<StringSequence2, ObjectSequence2, TAggregator>(), null);
                }

                return (FixedSizeLabelNameDictionary<TStringSequence, TObjectSequence, TAggregator>)(object)_label2;

            case StringSequence3:
                if (_label3 == null)
                {
                    Interlocked.CompareExchange(ref _label3, new FixedSizeLabelNameDictionary<StringSequence3, ObjectSequence3, TAggregator>(), null);
                }

                return (FixedSizeLabelNameDictionary<TStringSequence, TObjectSequence, TAggregator>)(object)_label3;

            case StringSequenceMany:
                if (_labelMany == null)
                {
                    Interlocked.CompareExchange(ref _labelMany, new FixedSizeLabelNameDictionary<StringSequenceMany, ObjectSequenceMany, TAggregator>(), null);
                }

                return (FixedSizeLabelNameDictionary<TStringSequence, TObjectSequence, TAggregator>)(object)_labelMany;

            default:
                // we should never get here unless this library has a bug
                Debug.Fail("Unexpected sequence type");
                return new FixedSizeLabelNameDictionary<TStringSequence, TObjectSequence, TAggregator>();
        }
    }

    public void Collect(Action<LabeledAggregationStatistics> visitFunc)
    {
        if (_noLabelAggregator != null)
        {
            IAggregationStatistics stats = _noLabelAggregator.Collect();
            visitFunc(new LabeledAggregationStatistics(stats));
        }

        _label1?.Collect(visitFunc);
        _label2?.Collect(visitFunc);
        _label3?.Collect(visitFunc);
        _labelMany?.Collect(visitFunc);
    }
}
