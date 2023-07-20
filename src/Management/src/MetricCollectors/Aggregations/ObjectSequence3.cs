// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

#pragma warning disable S2328 // "GetHashCode" should not reference mutable fields

namespace Steeltoe.Management.MetricCollectors.Aggregations;

internal struct ObjectSequence3 : IEquatable<ObjectSequence3>, IObjectSequence
{
    private object? _value1;
    private readonly object? _value2;
    private readonly object? _value3;

    public ObjectSequence3(object? value1, object? value2, object? value3)
    {
        _value1 = value1;
        _value2 = value2;
        _value3 = value3;
    }

    public Span<object?> AsSpan()
    {
        return MemoryMarshal.CreateSpan(ref _value1, 3);
    }

    public bool Equals(ObjectSequence3 other)
    {
        return (_value1?.Equals(other._value1) ?? other._value1 is null) && (_value2?.Equals(other._value2) ?? other._value2 is null) &&
            (_value3?.Equals(other._value3) ?? other._value3 is null);
    }

    public override bool Equals(object? obj)
    {
        return obj is ObjectSequence3 os3 && Equals(os3);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_value1, _value2, _value3);
    }
}
