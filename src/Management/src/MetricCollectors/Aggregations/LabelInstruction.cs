// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;

namespace Steeltoe.Management.MetricCollectors.Aggregations;

internal readonly struct LabelInstruction : IEquatable<LabelInstruction>
{
    public int SourceIndex { get; }
    public string LabelName { get; }

    public LabelInstruction(int sourceIndex, string labelName)
    {
        ArgumentGuard.NotNull(labelName);

        SourceIndex = sourceIndex;
        LabelName = labelName;
    }

    public bool Equals(LabelInstruction other)
    {
        return SourceIndex == other.SourceIndex && LabelName == other.LabelName;
    }

    public override bool Equals(object? obj)
    {
        return obj is LabelInstruction other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SourceIndex, LabelName);
    }
}
