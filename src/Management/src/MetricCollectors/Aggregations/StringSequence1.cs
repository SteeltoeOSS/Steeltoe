// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

#pragma warning disable S2328 // "GetHashCode" should not reference mutable fields

namespace Steeltoe.Management.MetricCollectors.Aggregations;

internal struct StringSequence1 : IEquatable<StringSequence1>, IStringSequence
{
    private string _value1;

    public StringSequence1(string value1)
    {
        _value1 = value1;
    }

    public Span<string> AsSpan()
    {
        return MemoryMarshal.CreateSpan(ref _value1, 1);
    }

    public bool Equals(StringSequence1 other)
    {
        return _value1 == other._value1;
    }

    public override bool Equals(object? obj)
    {
        return obj is StringSequence1 ss1 && Equals(ss1);
    }

    public override int GetHashCode()
    {
        return _value1.GetHashCode();
    }
}
