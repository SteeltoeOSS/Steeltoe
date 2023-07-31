// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Steeltoe.Common;

namespace Steeltoe.Management.MetricCollectors.Aggregations;

internal sealed class LabelInstructionInterpreter<TObjectSequence, TAggregator>
    where TObjectSequence : struct, IObjectSequence, IEquatable<TObjectSequence>
    where TAggregator : Aggregator
{
    private readonly int _expectedLabelCount;
    private readonly LabelInstruction[] _instructions;
    private readonly ConcurrentDictionary<TObjectSequence, TAggregator> _valuesDictionary;
    private readonly Func<TObjectSequence, TAggregator?> _createAggregator;

    public LabelInstructionInterpreter(int expectedLabelCount, LabelInstruction[] instructions,
        ConcurrentDictionary<TObjectSequence, TAggregator> valuesDictionary, Func<TAggregator?> createAggregator)
    {
        ArgumentGuard.NotNull(instructions);
        ArgumentGuard.NotNull(valuesDictionary);
        ArgumentGuard.NotNull(createAggregator);

        _expectedLabelCount = expectedLabelCount;
        _instructions = instructions;
        _valuesDictionary = valuesDictionary;
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

        for (int index = 0; index < _instructions.Length; index++)
        {
            LabelInstruction instr = _instructions[index];

            if (instr.LabelName != labels[instr.SourceIndex].Key)
            {
                return false;
            }

            indexedValues[index] = labels[instr.SourceIndex].Value;
        }

        if (!_valuesDictionary.TryGetValue(values, out aggregator))
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

            aggregator = _valuesDictionary.GetOrAdd(values, aggregator);
        }

        return true;
    }
}
