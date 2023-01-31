// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Numerics;
using System.Runtime.InteropServices;

namespace System.Diagnostics.Metrics;

internal partial struct StringSequence1 : IEquatable<StringSequence1>, IStringSequence
{
    public string Value1;

    public StringSequence1(string value1)
    {
        Value1 = value1;
    }

    [CodeAnalysis.SuppressMessage("Minor Bug", "S2328:\"GetHashCode\" should not reference mutable fields", Justification = "Value1 cannot be readonly since it is used as a ref param")]
    public override int GetHashCode() => Value1.GetHashCode();

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

    public StringSequenceMany(string[] values) =>
        _values = values;

    public Span<string> AsSpan() =>
        _values.AsSpan();

    public bool Equals(StringSequenceMany other) =>
        _values.AsSpan().SequenceEqual(other._values.AsSpan());

    // GetHashCode() is in the platform specific files
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public override bool Equals(object? obj) =>
        obj is StringSequenceMany ssm && Equals(ssm);
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
}
