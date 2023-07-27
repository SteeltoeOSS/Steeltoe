// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Steeltoe.Common;

#pragma warning disable S2328 // "GetHashCode" should not reference mutable fields

namespace Steeltoe.Management.MetricCollectors.Aggregations;

internal struct StringSequence2 : IEquatable<StringSequence2>, IStringSequence
{
    private string _value1;
    private readonly string _value2;

    public StringSequence2(string value1, string value2)
    {
        ArgumentGuard.NotNull(value1);
        ArgumentGuard.NotNull(value2);

        _value1 = value1;
        _value2 = value2;
    }

    public Span<string> AsSpan()
    {
        return MemoryMarshal.CreateSpan(ref _value1, 2);
    }

    public bool Equals(StringSequence2 other)
    {
        return _value1 == other._value1 && _value2 == other._value2;
    }

    public override bool Equals(object? obj)
    {
        return obj is StringSequence2 ss2 && Equals(ss2);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_value1, _value2);
    }
}
