// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.Common.Expression.Internal;

public class TypedValue : ITypedValue
{
    public static readonly TypedValue Null = new(null);

    public object Value { get; }

    public Type TypeDescriptor { get; }

    public TypedValue(object value)
    {
        Value = value;
        TypeDescriptor = value?.GetType();
    }

    public TypedValue(object value, Type typeDescriptor)
    {
        Value = value;
        TypeDescriptor = typeDescriptor;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not TypedValue otherTv)
        {
            return false;
        }

        return ObjectUtils.NullSafeEquals(Value, otherTv.Value) && ((TypeDescriptor == null && otherTv.TypeDescriptor == null) ||
            ObjectUtils.NullSafeEquals(TypeDescriptor, otherTv.TypeDescriptor));
    }

    public override int GetHashCode()
    {
        return ObjectUtils.NullSafeHashCode(Value);
    }

    public override string ToString()
    {
        return $"TypedValue: '{Value}' of [{TypeDescriptor}]";
    }
}
