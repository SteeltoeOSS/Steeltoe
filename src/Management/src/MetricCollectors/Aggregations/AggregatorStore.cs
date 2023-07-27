// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using Steeltoe.Common;

#pragma warning disable S2328 // "GetHashCode" should not reference mutable fields

namespace Steeltoe.Management.MetricCollectors.Aggregations;

/// <summary>
/// AggregatorStore is a high performance map from an unordered list of labels (KeyValuePairs) to an instance of TAggregator.
/// </summary>
/// <typeparam name="TAggregator">
/// The type of Aggregator returned by the store.
/// </typeparam>
//
// This is implemented as a two level Dictionary lookup with a number of optimizations applied. The conceptual lookup is:
// 1. Sort ReadOnlySpan<KeyValuePair<string,object?>> by the key names
// 2. Split ReadOnlySpan<KeyValuePair<string,object?>> into ReadOnlySpan<string> and ReadOnlySpan<object?>
// 3. LabelNameDictionary.Lookup(ReadOnlySpan<string>) -> ConcurrentDictionary
// 4. ConcurrentDictionary.Lookup(ReadOnlySpan<object?>) -> TAggregator
//
// There are several things we are optimizing for:
//   - CPU instructions per lookup: In the common case the key portion of the KeyValuePairs is unchanged between requests
//   and they are given in the same order. This means we can cache the 2nd level concurrent dictionary and the permutation that
//   will sort the labels as long as we determine the keys are unchanged from the previous request. The first time a new set of
//   keys is observed we call into LabelInstructionCompiler.Create which will determine the canonical sort order, do the 1st level
//   lookup, and then return a new _cachedLookupFunc. Invoking _cachedLookupFunc confirms the keys match what was previously
//   observed, re-orders the values with the cached permutation and performs the 2nd level lookup against the cached 2nd level
//   Dictionary. If we wanted to get really fancy we could have that compiler generate IL that would be JIT compiled, but right now
//   LabelInstructionCompiler simply creates a managed data structure (LabelInstructionInterpreter) that encodes the permutation
//   in an array of LabelInstructions and the 2nd level dictionary in another field. LabelInstructionInterpreter.GetAggregator
//   re-orders the values with a for loop and then does the lookup. Depending on ratio between fast-path and slow-path invocations
//   it may also not be a win to further pessimize the slow-path (JIT compilation is expensive) to squeeze yet more cycles out of
//   the fast path.
//   - Allocations per lookup: Any lookup of 3 or fewer labels on the above fast path is allocation free. We have separate
//   dictionaries depending on the number of labels in the list and the dictionary keys are structures representing fixed size
//   lists of strings or objects. For example with two labels the lookup is done in a
//   FixedSizeLabelNameDictionary<StringSequence2, ConcurrentDictionary<ObjectSequence2, TAggregator>>
//   Above 3 labels we have StringSequenceMany and ObjectSequenceMany which wraps an underlying string[] or object?[] respectively.
//   Doing a lookup with those types will need to do allocations for those arrays.
//   - Total memory footprint per-store: We have a store for every instrument we are tracking and an entry in the 2nd level
//   dictionary for every label set. This can add up to a lot of entries. Splitting the label sets into keys and values means we
//   only need to store each unique key list once (as the key of the 1st level dictionary). It is common for all label sets on an
//   instrument to have the same keys so this can be a sizable savings. We also use a union to store the 1st level dictionaries
//   for different label set sizes because most instruments always specify label sets with the same number of labels (most likely
//   zero).
internal struct AggregatorStore<TAggregator> : IEquatable<AggregatorStore<TAggregator>>
    where TAggregator : Aggregator
{
    // this union can be:
    // null
    // TAggregator
    // FixedSizeLabelNameDictionary<StringSequence1, ConcurrentDictionary<ObjectSequence1, TAggregator>>
    // FixedSizeLabelNameDictionary<StringSequence2, ConcurrentDictionary<ObjectSequence2, TAggregator>>
    // FixedSizeLabelNameDictionary<StringSequence3, ConcurrentDictionary<ObjectSequence3, TAggregator>>
    // FixedSizeLabelNameDictionary<StringSequenceMany, ConcurrentDictionary<ObjectSequenceMany, TAggregator>>
    // MultiSizeLabelNameDictionary<TAggregator> - this is used when we need to store more than one of the above union items
    private volatile object? _stateUnion;
    private volatile AggregatorLookupFunc<TAggregator>? _cachedLookupFunc;
    private readonly Func<TAggregator?> _createAggregatorFunc;

    public AggregatorStore(Func<TAggregator?> createAggregator)
    {
        ArgumentGuard.NotNull(createAggregator);

        _stateUnion = null;
        _cachedLookupFunc = null;
        _createAggregatorFunc = createAggregator;
    }

    public TAggregator? GetAggregator()
    {
        while (true)
        {
            object? state = _stateUnion;

            if (state == null)
            {
                // running this delegate will increment the counter for the number of time series
                // even though in the rare race condition we don't store it. If we wanted to be perfectly
                // accurate we need to decrement the counter again, but I don't think mitigating that
                // error is worth the complexity
                TAggregator? newState = _createAggregatorFunc();

                if (newState == null)
                {
                    return newState;
                }

                if (Interlocked.CompareExchange(ref _stateUnion, newState, null) is null)
                {
                    return newState;
                }

                continue;
            }

            if (state is TAggregator aggState)
            {
                return aggState;
            }

            if (state is MultiSizeLabelNameDictionary<TAggregator> multiSizeState)
            {
                return multiSizeState.GetNoLabelAggregator(_createAggregatorFunc);
            }

            MultiSizeLabelNameDictionary<TAggregator> newState2 = new(state);

            if (Interlocked.CompareExchange(ref _stateUnion, newState2, state) == state)
            {
                return newState2.GetNoLabelAggregator(_createAggregatorFunc);
            }
        }
    }

    public TAggregator? GetAggregator(ReadOnlySpan<KeyValuePair<string, object?>> labels)
    {
        AggregatorLookupFunc<TAggregator>? lookupFunc = _cachedLookupFunc;

        if (lookupFunc != null && lookupFunc(labels, out TAggregator? aggregator))
        {
            return aggregator;
        }

        // slow path, label names have changed from what the lookupFunc cached so we need to
        // rebuild it
        return GetAggregatorSlow(labels);
    }

    private TAggregator? GetAggregatorSlow(ReadOnlySpan<KeyValuePair<string, object?>> labels)
    {
        AggregatorLookupFunc<TAggregator> lookupFunc = LabelInstructionCompiler.Create(ref this, _createAggregatorFunc, labels);
        _cachedLookupFunc = lookupFunc;
        bool match = lookupFunc(labels, out TAggregator? aggregator);
        Debug.Assert(match, "Did not find a match");
        return aggregator;
    }

    public void Collect(Action<LabeledAggregationStatistics> visitFunc)
    {
        ArgumentGuard.NotNull(visitFunc);

        switch (_stateUnion)
        {
            case TAggregator agg:
                IAggregationStatistics stats = agg.Collect();
                visitFunc(new LabeledAggregationStatistics(stats));
                break;

            case FixedSizeLabelNameDictionary<StringSequence1, ObjectSequence1, TAggregator> aggregations1:
                aggregations1.Collect(visitFunc);
                break;

            case FixedSizeLabelNameDictionary<StringSequence2, ObjectSequence2, TAggregator> aggregations2:
                aggregations2.Collect(visitFunc);
                break;

            case FixedSizeLabelNameDictionary<StringSequence3, ObjectSequence3, TAggregator> aggregations3:
                aggregations3.Collect(visitFunc);
                break;

            case FixedSizeLabelNameDictionary<StringSequenceMany, ObjectSequenceMany, TAggregator> aggregationsMany:
                aggregationsMany.Collect(visitFunc);
                break;

            case MultiSizeLabelNameDictionary<TAggregator> aggregationsMultiSize:
                aggregationsMultiSize.Collect(visitFunc);
                break;
        }
    }

    public ConcurrentDictionary<TObjectSequence, TAggregator> GetLabelValuesDictionary<TStringSequence, TObjectSequence>(in TStringSequence names)
        where TStringSequence : IStringSequence, IEquatable<TStringSequence>
        where TObjectSequence : IObjectSequence, IEquatable<TObjectSequence>
    {
        while (true)
        {
            object? state = _stateUnion;

            if (state == null)
            {
                FixedSizeLabelNameDictionary<TStringSequence, TObjectSequence, TAggregator> newState = new();

                if (Interlocked.CompareExchange(ref _stateUnion, newState, null) is null)
                {
                    return newState.GetValuesDictionary(names);
                }

                continue;
            }

            if (state is FixedSizeLabelNameDictionary<TStringSequence, TObjectSequence, TAggregator> fixedState)
            {
                return fixedState.GetValuesDictionary(names);
            }

            if (state is MultiSizeLabelNameDictionary<TAggregator> multiSizeState)
            {
                return multiSizeState.GetFixedSizeLabelNameDictionary<TStringSequence, TObjectSequence>().GetValuesDictionary(names);
            }

            MultiSizeLabelNameDictionary<TAggregator> newState2 = new(state);

            if (Interlocked.CompareExchange(ref _stateUnion, newState2, state) == state)
            {
                return newState2.GetFixedSizeLabelNameDictionary<TStringSequence, TObjectSequence>().GetValuesDictionary(names);
            }
        }
    }

    public bool Equals(AggregatorStore<TAggregator> other)
    {
        return Equals(_stateUnion, other._stateUnion) && Equals(_cachedLookupFunc, other._cachedLookupFunc) &&
            _createAggregatorFunc.Equals(other._createAggregatorFunc);
    }

    public override bool Equals(object? obj)
    {
        return obj is AggregatorStore<TAggregator> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_stateUnion, _cachedLookupFunc, _createAggregatorFunc);
    }
}
