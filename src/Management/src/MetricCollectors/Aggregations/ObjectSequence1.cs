// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

#pragma warning disable S2328 // "GetHashCode" should not reference mutable fields

namespace Steeltoe.Management.MetricCollectors.Aggregations;

internal struct ObjectSequence1 : IEquatable<ObjectSequence1>, IObjectSequence
{
    private object? _value1;

    public ObjectSequence1(object? value1)
    {
        _value1 = value1;
    }

    public Span<object?> AsSpan()
    {
        return MemoryMarshal.CreateSpan(ref _value1, 1);
    }

    public bool Equals(ObjectSequence1 other)
    {
        return _value1 is null ? other._value1 is null : _value1.Equals(other._value1);
    }

    public override bool Equals(object? obj)
    {
        return obj is ObjectSequence1 os1 && Equals(os1);
    }

    public override int GetHashCode()
    {
        return _value1?.GetHashCode() ?? 0;
    }
}
