// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.MetricCollectors.Aggregations;

internal readonly struct ObjectSequenceMany : IEquatable<ObjectSequenceMany>, IObjectSequence
{
    private readonly object?[] _values;

    public ObjectSequenceMany(object[] values)
    {
        _values = values;
    }

    public Span<object?> AsSpan()
    {
        return _values.AsSpan();
    }

    public bool Equals(ObjectSequenceMany other)
    {
        if (_values.Length != other._values.Length)
        {
            return false;
        }

        for (int i = 0; i < _values.Length; i++)
        {
            object? value = _values[i];
            object? otherValue = other._values[i];

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

    public override bool Equals(object? obj)
    {
        return obj is ObjectSequenceMany osm && Equals(osm);
    }

    public override int GetHashCode()
    {
        HashCode hashCode = default;

        for (int index = 0; index < _values.Length; index++)
        {
            hashCode.Add(_values[index]);
        }

        return hashCode.ToHashCode();
    }
}
