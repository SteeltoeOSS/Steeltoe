// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Security;

namespace System.Diagnostics.Metrics;

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
//   dictionaries dependending on the number of labels in the list and the dictionary keys are structures representing fixed size
//   lists of strings or objects. For example with two labels the lookup is done in a
//   FixedSizeLabelNameDictionary<StringSequence2, ConcurrentDictionary<ObjectSequence2, TAggregator>>
//   Above 3 labels we have StringSequenceMany and ObjectSequenceMany which wraps an underlying string[] or object?[] respectively.
//   Doing a lookup with those types will need to do allocations for those arrays.
//   - Total memory footprint per-store: We have a store for every instrument we are tracking and an entry in the 2nd level
//   dictionary for every label set. This can add up to a lot of entries. Splitting the label sets into keys and values means we
//   only need to store each unique key list once (as the key of the 1st level dictionary). It is common for all labelsets on an
//   instrument to have the same keys so this can be a sizable savings. We also use a union to store the 1st level dictionaries
//   for different label set sizes because most instruments always specify labelsets with the same number of labels (most likely
//   zero).
[SecuritySafeCritical] // using SecurityCritical type ReadOnlySpan
#pragma warning disable S3898 // Value types should implement "IEquatable<T>"
internal struct AggregatorStore<TAggregator>
    where TAggregator : Aggregator
#pragma warning restore S3898 // Value types should implement "IEquatable<T>"
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
        _stateUnion = null;
        _cachedLookupFunc = null;
        _createAggregatorFunc = createAggregator;
    }

#pragma warning disable S4136 // Method overloads should be grouped together
    public TAggregator? GetAggregator(ReadOnlySpan<KeyValuePair<string, object?>> labels)
#pragma warning restore S4136 // Method overloads should be grouped together
    {
        AggregatorLookupFunc<TAggregator>? lookupFunc = _cachedLookupFunc;

        if (lookupFunc != null)
        {
#pragma warning disable S1066 // Collapsible "if" statements should be merged
            if (lookupFunc(labels, out TAggregator? aggregator))
            {
                return aggregator;
            }
#pragma warning restore S1066 // Collapsible "if" statements should be merged
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
        switch (_stateUnion)
        {
            case TAggregator agg:
                IAggregationStatistics stats = agg.Collect();
                visitFunc(new LabeledAggregationStatistics(stats));
                break;

            case FixedSizeLabelNameDictionary<StringSequence1, ObjectSequence1, TAggregator> aggs1:
                aggs1.Collect(visitFunc);
                break;

            case FixedSizeLabelNameDictionary<StringSequence2, ObjectSequence2, TAggregator> aggs2:
                aggs2.Collect(visitFunc);
                break;

            case FixedSizeLabelNameDictionary<StringSequence3, ObjectSequence3, TAggregator> aggs3:
                aggs3.Collect(visitFunc);
                break;

            case FixedSizeLabelNameDictionary<StringSequenceMany, ObjectSequenceMany, TAggregator> aggsMany:
                aggsMany.Collect(visitFunc);
                break;

            case MultiSizeLabelNameDictionary<TAggregator> aggsMultiSize:
                aggsMultiSize.Collect(visitFunc);
                break;
        }
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
            else if (state is TAggregator aggState)
            {
                return aggState;
            }
            else if (state is MultiSizeLabelNameDictionary<TAggregator> multiSizeState)
            {
                return multiSizeState.GetNoLabelAggregator(_createAggregatorFunc);
            }
            else
            {
                MultiSizeLabelNameDictionary<TAggregator> newState = new(state);

                if (Interlocked.CompareExchange(ref _stateUnion, newState, state) == state)
                {
                    return newState.GetNoLabelAggregator(_createAggregatorFunc);
                }

                continue;
            }
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
            else if (state is FixedSizeLabelNameDictionary<TStringSequence, TObjectSequence, TAggregator> fixedState)
            {
                return fixedState.GetValuesDictionary(names);
            }
            else if (state is MultiSizeLabelNameDictionary<TAggregator> multiSizeState)
            {
                return multiSizeState.GetFixedSizeLabelNameDictionary<TStringSequence, TObjectSequence>().GetValuesDictionary(names);
            }
            else
            {
                MultiSizeLabelNameDictionary<TAggregator> newState = new(state);

                if (Interlocked.CompareExchange(ref _stateUnion, newState, state) == state)
                {
                    return newState.GetFixedSizeLabelNameDictionary<TStringSequence, TObjectSequence>().GetValuesDictionary(names);
                }

                continue;
            }
        }
    }
}

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
                return null;
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

#pragma warning disable IDE0250 // Make struct 'readonly'
#pragma warning disable S3898 // Value types should implement "IEquatable<T>"
internal struct LabelInstruction
#pragma warning restore S3898 // Value types should implement "IEquatable<T>"
#pragma warning restore IDE0250 // Make struct 'readonly'
{
    public LabelInstruction(int sourceIndex, string labelName)
    {
        SourceIndex = sourceIndex;
        LabelName = labelName;
    }

    public readonly int SourceIndex { get; }
    public readonly string LabelName { get; }
}

internal delegate bool AggregatorLookupFunc<TAggregator>(ReadOnlySpan<KeyValuePair<string, object?>> labels, out TAggregator? aggregator);

[SecurityCritical] // using SecurityCritical type ReadOnlySpan
internal static class LabelInstructionCompiler
{
    public static AggregatorLookupFunc<TAggregator> Create<TAggregator>(ref AggregatorStore<TAggregator> aggregatorStore,
        Func<TAggregator?> createAggregatorFunc, ReadOnlySpan<KeyValuePair<string, object?>> labels)
        where TAggregator : Aggregator
    {
        LabelInstruction[] instructions = Compile(labels);
        Array.Sort(instructions, (LabelInstruction a, LabelInstruction b) => string.CompareOrdinal(a.LabelName, b.LabelName));
        int expectedLabels = labels.Length;

        switch (instructions.Length)
        {
            case 0:
                TAggregator? defaultAggregator = aggregatorStore.GetAggregator();

                return (ReadOnlySpan<KeyValuePair<string, object?>> l, out TAggregator? aggregator) =>
                {
                    if (l.Length != expectedLabels)
                    {
                        aggregator = null;
                        return false;
                    }

                    aggregator = defaultAggregator;
                    return true;
                };

            case 1:
                StringSequence1 names1 = new StringSequence1(instructions[0].LabelName);

                ConcurrentDictionary<ObjectSequence1, TAggregator> valuesDict1 =
                    aggregatorStore.GetLabelValuesDictionary<StringSequence1, ObjectSequence1>(names1);

                LabelInstructionInterpreter<ObjectSequence1, TAggregator> interpreter1 =
                    new LabelInstructionInterpreter<ObjectSequence1, TAggregator>(expectedLabels, instructions, valuesDict1, createAggregatorFunc);

                return interpreter1.GetAggregator;

            case 2:
                StringSequence2 names2 = new StringSequence2(instructions[0].LabelName, instructions[1].LabelName);

                ConcurrentDictionary<ObjectSequence2, TAggregator> valuesDict2 =
                    aggregatorStore.GetLabelValuesDictionary<StringSequence2, ObjectSequence2>(names2);

                LabelInstructionInterpreter<ObjectSequence2, TAggregator> interpreter2 =
                    new LabelInstructionInterpreter<ObjectSequence2, TAggregator>(expectedLabels, instructions, valuesDict2, createAggregatorFunc);

                return interpreter2.GetAggregator;

            case 3:
                StringSequence3 names3 = new StringSequence3(instructions[0].LabelName, instructions[1].LabelName, instructions[2].LabelName);

                ConcurrentDictionary<ObjectSequence3, TAggregator> valuesDict3 =
                    aggregatorStore.GetLabelValuesDictionary<StringSequence3, ObjectSequence3>(names3);

                LabelInstructionInterpreter<ObjectSequence3, TAggregator> interpreter3 =
                    new LabelInstructionInterpreter<ObjectSequence3, TAggregator>(expectedLabels, instructions, valuesDict3, createAggregatorFunc);

                return interpreter3.GetAggregator;

            default:
                string[] labelNames = new string[instructions.Length];

                for (int i = 0; i < instructions.Length; i++)
                {
                    labelNames[i] = instructions[i].LabelName;
                }

                StringSequenceMany namesMany = new StringSequenceMany(labelNames);

                ConcurrentDictionary<ObjectSequenceMany, TAggregator> valuesDictMany =
                    aggregatorStore.GetLabelValuesDictionary<StringSequenceMany, ObjectSequenceMany>(namesMany);

                LabelInstructionInterpreter<ObjectSequenceMany, TAggregator> interpreter4 =
                    new LabelInstructionInterpreter<ObjectSequenceMany, TAggregator>(expectedLabels, instructions, valuesDictMany, createAggregatorFunc);

                return interpreter4.GetAggregator;
        }
    }

    private static LabelInstruction[] Compile(ReadOnlySpan<KeyValuePair<string, object?>> labels)
    {
        LabelInstruction[] valueFetches = new LabelInstruction[labels.Length];

        for (int i = 0; i < labels.Length; i++)
        {
            valueFetches[i] = new LabelInstruction(i, labels[i].Key);
        }

        return valueFetches;
    }
}

[SecurityCritical] // using SecurityCritical type ReadOnlySpan
internal sealed class LabelInstructionInterpreter<TObjectSequence, TAggregator>
    where TObjectSequence : struct, IObjectSequence, IEquatable<TObjectSequence>
    where TAggregator : Aggregator
{
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable S2933 // Fields that are only assigned in the constructor should be "readonly"
    private int _expectedLabelCount;
#pragma warning restore S2933 // Fields that are only assigned in the constructor should be "readonly"
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable S2933 // Fields that are only assigned in the constructor should be "readonly"
    private LabelInstruction[] _instructions;
#pragma warning restore S2933 // Fields that are only assigned in the constructor should be "readonly"
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable S2933 // Fields that are only assigned in the constructor should be "readonly"
    private ConcurrentDictionary<TObjectSequence, TAggregator> _valuesDict;
#pragma warning restore S2933 // Fields that are only assigned in the constructor should be "readonly"
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable S2933 // Fields that are only assigned in the constructor should be "readonly"
    private Func<TObjectSequence, TAggregator?> _createAggregator;
#pragma warning restore S2933 // Fields that are only assigned in the constructor should be "readonly"
#pragma warning restore IDE0044 // Add readonly modifier

    public LabelInstructionInterpreter(int expectedLabelCount, LabelInstruction[] instructions, ConcurrentDictionary<TObjectSequence, TAggregator> valuesDict,
        Func<TAggregator?> createAggregator)
    {
        _expectedLabelCount = expectedLabelCount;
        _instructions = instructions;
        _valuesDict = valuesDict;
        _createAggregator = _ => createAggregator();
    }

    // Returns true if label keys matched what was expected
    // aggregator may be null even when true is returned if
    // we have hit the storage limits
    public bool GetAggregator(ReadOnlySpan<KeyValuePair<string, object?>> labels, out TAggregator? aggregator)
    {
        aggregator = null;

        if (labels.Length != _expectedLabelCount)
        {
            return false;
        }

        TObjectSequence values = default;

        if (values is ObjectSequenceMany)
        {
            values = (TObjectSequence)(object)new ObjectSequenceMany(new object[_expectedLabelCount]);
        }

        Span<object?> indexedValues = values.AsSpan();

        for (int i = 0; i < _instructions.Length; i++)
        {
            LabelInstruction instr = _instructions[i];

            if (instr.LabelName != labels[instr.SourceIndex].Key)
            {
                return false;
            }

            indexedValues[i] = labels[instr.SourceIndex].Value;
        }

        if (!_valuesDict.TryGetValue(values, out aggregator))
        {
            // running this delegate will increment the counter for the number of time series
            // even though in the rare race condition we don't store it. If we wanted to be perfectly
            // accurate we need to decrement the counter again, but I don't think mitigating that
            // error is worth the complexity
            aggregator = _createAggregator(values);

            if (aggregator is null)
            {
                return true;
            }

            aggregator = _valuesDict.GetOrAdd(values, aggregator);
        }

        return true;
    }
}

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
                KeyValuePair<string, string>[] labels = new KeyValuePair<string, string>[indexedNames.Length];

                for (int i = 0; i < labels.Length; i++)
                {
                    labels[i] = new KeyValuePair<string, string>(indexedNames[i], indexedValues[i]?.ToString() ?? string.Empty);
                }

                IAggregationStatistics stats = kvValue.Value.Collect();
                visitFunc(new LabeledAggregationStatistics(stats, labels));
            }
        }
    }

    public ConcurrentDictionary<TObjectSequence, TAggregator> GetValuesDictionary(in TStringSequence names) =>
        GetOrAdd(names, _ => new ConcurrentDictionary<TObjectSequence, TAggregator>());
}
