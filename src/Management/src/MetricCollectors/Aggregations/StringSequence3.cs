// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

#pragma warning disable S2328 // "GetHashCode" should not reference mutable fields

namespace Steeltoe.Management.MetricCollectors.Aggregations;

internal struct StringSequence3 : IEquatable<StringSequence3>, IStringSequence
{
    private string _value1;
    private readonly string _value2;
    private readonly string _value3;

    public StringSequence3(string value1, string value2, string value3)
    {
        _value1 = value1;
        _value2 = value2;
        _value3 = value3;
    }

    public Span<string> AsSpan()
    {
        return MemoryMarshal.CreateSpan(ref _value1, 3);
    }

    public bool Equals(StringSequence3 other)
    {
        return _value1 == other._value1 && _value2 == other._value2 && _value3 == other._value3;
    }

    public override bool Equals(object? obj)
    {
        return obj is StringSequence3 ss3 && Equals(ss3);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_value1, _value2, _value3);
    }
}
