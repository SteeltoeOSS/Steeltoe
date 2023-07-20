// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Security;

namespace Steeltoe.Management.MetricCollectors.Aggregations;

[SecurityCritical] // using SecurityCritical type ReadOnlySpan
internal static class LabelInstructionCompiler
{
    public static AggregatorLookupFunc<TAggregator> Create<TAggregator>(ref AggregatorStore<TAggregator> aggregatorStore,
        Func<TAggregator?> createAggregatorFunc, ReadOnlySpan<KeyValuePair<string, object?>> labels)
        where TAggregator : Aggregator
    {
        LabelInstruction[] instructions = Compile(labels);
        Array.Sort(instructions, (a, b) => string.CompareOrdinal(a.LabelName, b.LabelName));
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
                var names1 = new StringSequence1(instructions[0].LabelName);

                ConcurrentDictionary<ObjectSequence1, TAggregator> valuesDict1 =
                    aggregatorStore.GetLabelValuesDictionary<StringSequence1, ObjectSequence1>(names1);

                var interpreter1 =
                    new LabelInstructionInterpreter<ObjectSequence1, TAggregator>(expectedLabels, instructions, valuesDict1, createAggregatorFunc);

                return interpreter1.GetAggregator;

            case 2:
                var names2 = new StringSequence2(instructions[0].LabelName, instructions[1].LabelName);

                ConcurrentDictionary<ObjectSequence2, TAggregator> valuesDict2 =
                    aggregatorStore.GetLabelValuesDictionary<StringSequence2, ObjectSequence2>(names2);

                var interpreter2 =
                    new LabelInstructionInterpreter<ObjectSequence2, TAggregator>(expectedLabels, instructions, valuesDict2, createAggregatorFunc);

                return interpreter2.GetAggregator;

            case 3:
                var names3 = new StringSequence3(instructions[0].LabelName, instructions[1].LabelName, instructions[2].LabelName);

                ConcurrentDictionary<ObjectSequence3, TAggregator> valuesDict3 =
                    aggregatorStore.GetLabelValuesDictionary<StringSequence3, ObjectSequence3>(names3);

                var interpreter3 =
                    new LabelInstructionInterpreter<ObjectSequence3, TAggregator>(expectedLabels, instructions, valuesDict3, createAggregatorFunc);

                return interpreter3.GetAggregator;

            default:
                string[] labelNames = new string[instructions.Length];

                for (int i = 0; i < instructions.Length; i++)
                {
                    labelNames[i] = instructions[i].LabelName;
                }

                var namesMany = new StringSequenceMany(labelNames);

                ConcurrentDictionary<ObjectSequenceMany, TAggregator> valuesDictMany =
                    aggregatorStore.GetLabelValuesDictionary<StringSequenceMany, ObjectSequenceMany>(namesMany);

                var interpreter4 =
                    new LabelInstructionInterpreter<ObjectSequenceMany, TAggregator>(expectedLabels, instructions, valuesDictMany, createAggregatorFunc);

                return interpreter4.GetAggregator;
        }
    }

    private static LabelInstruction[] Compile(ReadOnlySpan<KeyValuePair<string, object?>> labels)
    {
        var valueFetches = new LabelInstruction[labels.Length];

        for (int i = 0; i < labels.Length; i++)
        {
            valueFetches[i] = new LabelInstruction(i, labels[i].Key);
        }

        return valueFetches;
    }
}
