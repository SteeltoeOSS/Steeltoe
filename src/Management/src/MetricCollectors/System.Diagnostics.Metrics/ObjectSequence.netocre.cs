// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Diagnostics.Metrics;

internal interface IObjectSequence
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    Span<object?> AsSpan();
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
}

internal partial struct ObjectSequence1 : IEquatable<ObjectSequence1>, IObjectSequence
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public Span<object?> AsSpan()
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        return MemoryMarshal.CreateSpan(ref Value1, 1);
    }
}

internal partial struct ObjectSequence2 : IEquatable<ObjectSequence2>, IObjectSequence
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public Span<object?> AsSpan()
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        return MemoryMarshal.CreateSpan(ref Value1, 2);
    }

#pragma warning disable S2328 // "GetHashCode" should not reference mutable fields
    public override int GetHashCode()
    {
        return HashCode.Combine(Value1, Value2);
    }
#pragma warning restore S2328 // "GetHashCode" should not reference mutable fields
}

internal partial struct ObjectSequence3 : IEquatable<ObjectSequence3>, IObjectSequence
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public Span<object?> AsSpan()
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        return MemoryMarshal.CreateSpan(ref Value1, 3);
    }

#pragma warning disable S2328 // "GetHashCode" should not reference mutable fields
    public override int GetHashCode()
    {
        return HashCode.Combine(Value1, Value2, Value3);
    }
#pragma warning restore S2328 // "GetHashCode" should not reference mutable fields
}

#pragma warning disable IDE0250 // Make struct 'readonly'
internal partial struct ObjectSequenceMany : IEquatable<ObjectSequenceMany>, IObjectSequence
#pragma warning restore IDE0250 // Make struct 'readonly'
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public Span<object?> AsSpan()
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        return _values.AsSpan();
    }

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
