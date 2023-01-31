// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Numerics;
using System.Runtime.InteropServices;

namespace System.Diagnostics.Metrics;

internal interface IStringSequence
{
    Span<string> AsSpan();
}

internal partial struct StringSequence1 : IEquatable<StringSequence1>, IStringSequence
{

    public Span<string> AsSpan()
    {
        return MemoryMarshal.CreateSpan(ref Value1, 1);
    }
}

internal partial struct StringSequence2 : IEquatable<StringSequence2>, IStringSequence
{
    public Span<string> AsSpan()
    {
        return MemoryMarshal.CreateSpan(ref Value1, 2);
    }

#pragma warning disable S2328 // "GetHashCode" should not reference mutable fields
    public override int GetHashCode() => HashCode.Combine(Value1, Value2);
#pragma warning restore S2328 // "GetHashCode" should not reference mutable fields
}

internal partial struct StringSequence3 : IEquatable<StringSequence3>, IStringSequence
{
    public Span<string> AsSpan()
    {
        return MemoryMarshal.CreateSpan(ref Value1, 3);
    }

#pragma warning disable S2328 // "GetHashCode" should not reference mutable fields
    public override int GetHashCode() => HashCode.Combine(Value1, Value2, Value3);
#pragma warning restore S2328 // "GetHashCode" should not reference mutable fields
}

internal partial struct StringSequenceMany : IEquatable<StringSequenceMany>, IStringSequence
{
    public override int GetHashCode()
    {
        HashCode h = default;
        for (int i = 0; i < _values.Length; i++)
        {
            h.Add(_values[i]);
        }
        return h.ToHashCode();
    }
}
