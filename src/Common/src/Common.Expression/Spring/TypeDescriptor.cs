// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal.Spring;

public class TypeDescriptor
{
    public static readonly TypeDescriptor V = new (typeof(void));
    public static readonly TypeDescriptor I = new (typeof(int));
    public static readonly TypeDescriptor J = new (typeof(long));
    public static readonly TypeDescriptor F = new (typeof(float));
    public static readonly TypeDescriptor D = new (typeof(double));
    public static readonly TypeDescriptor B = new (typeof(byte));
    public static readonly TypeDescriptor C = new (typeof(char));
    public static readonly TypeDescriptor S = new (typeof(short));
    public static readonly TypeDescriptor Z = new (typeof(bool));
    public static readonly TypeDescriptor A = new (typeof(sbyte));
    public static readonly TypeDescriptor M = new (typeof(ushort));
    public static readonly TypeDescriptor N = new (typeof(uint));
    public static readonly TypeDescriptor O = new (typeof(ulong));
    public static readonly TypeDescriptor P = new (typeof(IntPtr));
    public static readonly TypeDescriptor Q = new (typeof(UIntPtr));

    public static readonly TypeDescriptor Object = new (typeof(object));
    public static readonly TypeDescriptor String = new (typeof(string));
    public static readonly TypeDescriptor Type = new (typeof(Type));

    private TypeDescriptor _boxed;
    private TypeDescriptor _unBoxed;

    public TypeDescriptor(Type type, bool boxed = false)
    {
        Value = type;
        IsBoxed = boxed;
    }

    public Type Value { get; }

    public bool IsBoxed { get; }

    public bool IsValueType => Value.IsValueType;  // Returns true for typeof(void)

    public bool IsReferenceType => !IsValueType && !IsBoxed;

    public bool IsBoxedValueType => IsValueType && IsBoxed;

    public bool IsPrimitive => !IsBoxed && !IsVoid && Value.IsPrimitive; // IsPrimitive returns false for typeof(void)

    public bool IsBoxedPrimitive => IsBoxed && Value.IsPrimitive;

    public bool IsBoxedNumber => IsBoxedPrimitive && Value != typeof(IntPtr) && Value != typeof(UIntPtr);

    public bool IsVoid => Value == typeof(void);

    public bool IsBoxable => IsValueType && !IsBoxed && !IsVoid;

    public TypeDescriptor UnBox()
    {
        if (!IsBoxed)
        {
            throw new InvalidOperationException("TypeDescriptor is not boxed");
        }

        if (_unBoxed == null)
        {
            _unBoxed = new TypeDescriptor(Value)
            {
                _boxed = this
            };
        }

        return _unBoxed;
    }

    public TypeDescriptor Boxed()
    {
        if (!IsBoxable)
        {
            throw new InvalidOperationException("Type not boxable");
        }

        if (_boxed == null)
        {
            _boxed = new TypeDescriptor(Value, true)
            {
                _unBoxed = this
            };
        }

        return _boxed;
    }

#pragma warning disable S3875 // "operator==" should not be overloaded on reference types
    public static bool operator ==(TypeDescriptor lhs, TypeDescriptor rhs)
    {
        if (lhs is null)
        {
            if (rhs is null)
            {
                return true;
            }

            return false;
        }

        return lhs.Equals(rhs);
    }

    public static bool operator !=(TypeDescriptor lhs, TypeDescriptor rhs)
    {
        return !(lhs == rhs);
    }
#pragma warning restore S3875 // "operator==" should not be overloaded on reference types

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not TypeDescriptor other)
        {
            return false;
        }

        return other.Value == Value &&
               other.IsBoxed == IsBoxed;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
