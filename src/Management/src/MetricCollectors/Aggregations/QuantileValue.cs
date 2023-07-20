// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.MetricCollectors.Aggregations;

internal readonly struct QuantileValue : IEquatable<QuantileValue>
{
    private readonly double _quantile;
    private readonly double _value;

    public QuantileValue(double quantile, double value)
    {
        _quantile = quantile;
        _value = value;
    }

    public bool Equals(QuantileValue other)
    {
        return _quantile.Equals(other._quantile) && _value.Equals(other._value);
    }

    public override bool Equals(object? obj)
    {
        return obj is QuantileValue other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_quantile, _value);
    }
}
