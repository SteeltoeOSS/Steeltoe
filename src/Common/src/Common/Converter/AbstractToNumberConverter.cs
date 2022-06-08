// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Converter;

public abstract class AbstractToNumberConverter : AbstractGenericConditionalConverter
{
    protected ISet<(Type Source, Type Target)> _convertableTypes;

    protected AbstractToNumberConverter(ISet<(Type Source, Type Target)> convertableTypes)
        : base(null)
    {
        _convertableTypes = convertableTypes;
    }

    public override bool Matches(Type sourceType, Type targetType)
    {
        var targetCheck = ConversionUtils.GetNullableElementType(targetType);
        var pair = (sourceType, targetCheck);
        return _convertableTypes.Contains(pair);
    }

    public override object Convert(object source, Type sourceType, Type targetType)
    {
        targetType = ConversionUtils.GetNullableElementType(targetType);
        if (typeof(int) == targetType)
        {
            return System.Convert.ToInt32(source);
        }
        else if (typeof(float) == targetType)
        {
            return System.Convert.ToSingle(source);
        }
        else if (typeof(uint) == targetType)
        {
            return System.Convert.ToUInt32(source);
        }
        else if (typeof(ulong) == targetType)
        {
            return System.Convert.ToUInt64(source);
        }
        else if (typeof(long) == targetType)
        {
            return System.Convert.ToInt64(source);
        }
        else if (typeof(double) == targetType)
        {
            return System.Convert.ToDouble(source);
        }
        else if (typeof(short) == targetType)
        {
            return System.Convert.ToInt16(source);
        }
        else if (typeof(ushort) == targetType)
        {
            return System.Convert.ToUInt16(source);
        }
        else if (typeof(decimal) == targetType)
        {
            return System.Convert.ToDecimal(source);
        }
        else if (typeof(byte) == targetType)
        {
            return System.Convert.ToByte(source);
        }
        else if (typeof(sbyte) == targetType)
        {
            return System.Convert.ToSByte(source);
        }

        throw new ArgumentException(nameof(targetType));
    }
}
