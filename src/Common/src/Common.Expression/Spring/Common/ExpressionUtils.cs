// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;

namespace Steeltoe.Common.Expression.Internal.Spring.Common;

public static class ExpressionUtils
{
    public static T ConvertTypedValue<T>(IEvaluationContext context, ITypedValue typedValue)
    {
        return (T)ConvertTypedValue(context, typedValue, typeof(T));
    }

    public static object ConvertTypedValue(IEvaluationContext context, ITypedValue typedValue, Type targetType)
    {
        var value = typedValue.Value;
        if (targetType == null)
        {
            return value;
        }

        if (context != null)
        {
            return context.TypeConverter.ConvertValue(value, typedValue.TypeDescriptor, targetType);
        }

        if (ClassUtils.IsAssignableValue(targetType, value))
        {
            return value;
        }

        throw new EvaluationException("Cannot convert value '" + value + "' to type '" + targetType.FullName + "'");
    }

    public static int ToInt(ITypeConverter typeConverter, ITypedValue typedValue)
    {
        return ConvertValue<int>(typeConverter, typedValue);
    }

    public static uint ToUInt(ITypeConverter typeConverter, ITypedValue typedValue)
    {
        return ConvertValue<uint>(typeConverter, typedValue);
    }

    public static bool ToBoolean(ITypeConverter typeConverter, ITypedValue typedValue)
    {
        return ConvertValue<bool>(typeConverter, typedValue);
    }

    public static double ToDouble(ITypeConverter typeConverter, ITypedValue typedValue)
    {
        return ConvertValue<double>(typeConverter, typedValue);
    }

    public static long ToLong(ITypeConverter typeConverter, ITypedValue typedValue)
    {
        return ConvertValue<long>(typeConverter, typedValue);
    }

    public static ulong ToULong(ITypeConverter typeConverter, ITypedValue typedValue)
    {
        return ConvertValue<ulong>(typeConverter, typedValue);
    }

    public static char ToChar(ITypeConverter typeConverter, ITypedValue typedValue)
    {
        return ConvertValue<char>(typeConverter, typedValue);
    }

    public static short ToShort(ITypeConverter typeConverter, ITypedValue typedValue)
    {
        return ConvertValue<short>(typeConverter, typedValue);
    }

    public static ushort ToUShort(ITypeConverter typeConverter, ITypedValue typedValue)
    {
        return ConvertValue<ushort>(typeConverter, typedValue);
    }

    public static float ToFloat(ITypeConverter typeConverter, ITypedValue typedValue)
    {
        return ConvertValue<float>(typeConverter, typedValue);
    }

    public static byte ToByte(ITypeConverter typeConverter, ITypedValue typedValue)
    {
        return ConvertValue<byte>(typeConverter, typedValue);
    }

    public static sbyte ToSByte(ITypeConverter typeConverter, ITypedValue typedValue)
    {
        return ConvertValue<sbyte>(typeConverter, typedValue);
    }

    private static T ConvertValue<T>(ITypeConverter typeConverter, ITypedValue typedValue)
    {
        var result = typeConverter.ConvertValue(typedValue.Value, typedValue.TypeDescriptor, typeof(T));
        if (result == null)
        {
            throw new InvalidOperationException("Null conversion result for value [" + typedValue.Value + "]");
        }

        return (T)result;
    }
}