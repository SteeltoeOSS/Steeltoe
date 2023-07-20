// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

#pragma warning disable S2328 // "GetHashCode" should not reference mutable fields

namespace Steeltoe.Management.MetricCollectors.Aggregations;

internal struct ObjectSequence2 : IEquatable<ObjectSequence2>, IObjectSequence
{
    private object? _value1;
    private readonly object? _value2;

    public ObjectSequence2(object? value1, object? value2)
    {
        _value1 = value1;
        _value2 = value2;
    }

    public Span<object?> AsSpan()
    {
        return MemoryMarshal.CreateSpan(ref _value1, 2);
    }

    public bool Equals(ObjectSequence2 other)
    {
        return (_value1?.Equals(other._value1) ?? other._value1 is null) && (_value2?.Equals(other._value2) ?? other._value2 is null);
    }

    public override bool Equals(object? obj)
    {
        return obj is ObjectSequence2 os2 && Equals(os2);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_value1, _value2);
    }
}
