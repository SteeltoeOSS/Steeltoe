// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.MetricCollectors.Aggregations;

internal readonly struct StringSequenceMany : IEquatable<StringSequenceMany>, IStringSequence
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

    public override bool Equals(object? obj)
    {
        return obj is StringSequenceMany ssm && Equals(ssm);
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
