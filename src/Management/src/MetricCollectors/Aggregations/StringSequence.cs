// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Steeltoe.Management.MetricCollectors.Aggregations;

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
    public override int GetHashCode()
    {
        return HashCode.Combine(Value1, Value2);
    }
#pragma warning restore S2328 // "GetHashCode" should not reference mutable fields
}

internal partial struct StringSequence3 : IEquatable<StringSequence3>, IStringSequence
{
    public Span<string> AsSpan()
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
internal partial struct StringSequence1 : IEquatable<StringSequence1>, IStringSequence
{
    public string Value1;

    public StringSequence1(string value1)
    {
        Value1 = value1;
    }

    [SuppressMessage("Minor Bug", "S2328:\"GetHashCode\" should not reference mutable fields",
        Justification = "Value1 cannot be readonly since it is used as a ref param")]
    public override int GetHashCode()
    {
        return Value1.GetHashCode();
    }

    public bool Equals(StringSequence1 other)
    {
        return Value1 == other.Value1;
    }

    // GetHashCode() is in the platform specific files
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public override bool Equals(object? obj)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        return obj is StringSequence1 ss1 && Equals(ss1);
    }
}

internal partial struct StringSequence2 : IEquatable<StringSequence2>, IStringSequence
{
    public string Value1;
    public string Value2;

    public StringSequence2(string value1, string value2)
    {
        Value1 = value1;
        Value2 = value2;
    }

    public bool Equals(StringSequence2 other)
    {
        return Value1 == other.Value1 && Value2 == other.Value2;
    }

    // GetHashCode() is in the platform specific files
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public override bool Equals(object? obj)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        return obj is StringSequence2 ss2 && Equals(ss2);
    }
}

internal partial struct StringSequence3 : IEquatable<StringSequence3>, IStringSequence
{
    public string Value1;
    public string Value2;
    public string Value3;

    public StringSequence3(string value1, string value2, string value3)
    {
        Value1 = value1;
        Value2 = value2;
        Value3 = value3;
    }

    public bool Equals(StringSequence3 other)
    {
        return Value1 == other.Value1 && Value2 == other.Value2 && Value3 == other.Value3;
    }

    // GetHashCode() is in the platform specific files
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public override bool Equals(object? obj)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        return obj is StringSequence3 ss3 && Equals(ss3);
    }
}

#pragma warning disable IDE0250 // Make struct 'readonly'
internal partial struct StringSequenceMany : IEquatable<StringSequenceMany>, IStringSequence
#pragma warning restore IDE0250 // Make struct 'readonly'
{
    private readonly string[] _values;

    public StringSequenceMany(string[] values)
    {
        _values = values;
    }

    public Span<string> AsSpan()
    {
        return _values.AsSpan();
    }

    public bool Equals(StringSequenceMany other)
    {
        return _values.AsSpan().SequenceEqual(other._values.AsSpan());
    }

    // GetHashCode() is in the platform specific files
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public override bool Equals(object? obj)
    {
        return obj is StringSequenceMany ssm && Equals(ssm);
    }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
}
