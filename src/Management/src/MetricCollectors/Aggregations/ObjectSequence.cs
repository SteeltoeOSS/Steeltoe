// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace Steeltoe.Management.MetricCollectors.Aggregations;

internal interface IObjectSequence
{
    Span<object?> AsSpan();
}

internal partial struct ObjectSequence1 : IEquatable<ObjectSequence1>, IObjectSequence
{
    public Span<object?> AsSpan()
    {
        return MemoryMarshal.CreateSpan(ref Value1, 1);
    }
}

internal partial struct ObjectSequence2 : IEquatable<ObjectSequence2>, IObjectSequence
{
    public Span<object?> AsSpan()
    {
        return MemoryMarshal.CreateSpan(ref Value1, 2);
    }

#pragma warning disable S2328
    public override int GetHashCode()
#pragma warning restore S2328
    {
        return HashCode.Combine(Value1, Value2);
    }
}

internal partial struct ObjectSequence3 : IEquatable<ObjectSequence3>, IObjectSequence
{
    public Span<object?> AsSpan()
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
    public Span<object?> AsSpan()
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

internal partial struct ObjectSequence1 : IEquatable<ObjectSequence1>, IObjectSequence
{
    public object? Value1;

    public ObjectSequence1(object? value1)
    {
        Value1 = value1;
    }

#pragma warning disable S2328 // "GetHashCode" should not reference mutable fields
    public override int GetHashCode()
    {
        return Value1?.GetHashCode() ?? 0;
    }
#pragma warning restore S2328 // "GetHashCode" should not reference mutable fields

    public bool Equals(ObjectSequence1 other)
    {
        return Value1 is null ? other.Value1 is null : Value1.Equals(other.Value1);
    }

    // GetHashCode() is in the platform specific files
    public override bool Equals(object? obj)
    {
        return obj is ObjectSequence1 os1 && Equals(os1);
    }
}

internal partial struct ObjectSequence2 : IEquatable<ObjectSequence2>, IObjectSequence
{
    public object? Value1;
    public object? Value2;

    public ObjectSequence2(object? value1, object? value2)
    {
        Value1 = value1;
        Value2 = value2;
    }

    public bool Equals(ObjectSequence2 other)
    {
        return (Value1 is null ? other.Value1 is null : Value1.Equals(other.Value1)) && (Value2 is null ? other.Value2 is null : Value2.Equals(other.Value2));
    }

    // GetHashCode() is in the platform specific files
    public override bool Equals(object? obj)
    {
        return obj is ObjectSequence2 os2 && Equals(os2);
    }
}

internal partial struct ObjectSequence3 : IEquatable<ObjectSequence3>, IObjectSequence
{
    public object? Value1;
    public object? Value2;
    public object? Value3;

    public ObjectSequence3(object? value1, object? value2, object? value3)
    {
        Value1 = value1;
        Value2 = value2;
        Value3 = value3;
    }

    public bool Equals(ObjectSequence3 other)
    {
#pragma warning disable S1067 // Expressions should not be too complex
        return (Value1 is null ? other.Value1 is null : Value1.Equals(other.Value1)) && (Value2 is null ? other.Value2 is null : Value2.Equals(other.Value2)) &&
            (Value3 is null ? other.Value3 is null : Value3.Equals(other.Value3));
#pragma warning restore S1067 // Expressions should not be too complex
    }

    // GetHashCode() is in the platform specific files
    public override bool Equals(object? obj)
    {
        return obj is ObjectSequence3 os3 && Equals(os3);
    }
}

internal partial struct ObjectSequenceMany : IEquatable<ObjectSequenceMany>, IObjectSequence
{
    private readonly object?[] _values;

    public ObjectSequenceMany(object[] values)
    {
        _values = values;
    }

    public bool Equals(ObjectSequenceMany other)
    {
        if (_values.Length != other._values.Length)
        {
            return false;
        }

        for (int i = 0; i < _values.Length; i++)
        {
#pragma warning disable S1659 // Multiple variables should not be declared on the same line
            object? value = _values[i], otherValue = other._values[i];
#pragma warning restore S1659 // Multiple variables should not be declared on the same line
            if (value is null)
            {
                if (otherValue is not null)
                {
                    return false;
                }
            }
            else if (!value.Equals(otherValue))
            {
                return false;
            }
        }

        return true;
    }

    // GetHashCode() is in the platform specific files
    public override bool Equals(object? obj)
    {
        return obj is ObjectSequenceMany osm && Equals(osm);
    }
}
