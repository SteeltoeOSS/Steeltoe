// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Converter;

public abstract class AbstractToNumberConverter : AbstractGenericConditionalConverter
{
    protected ISet<(Type SourceType, Type TargetType)> convertableTypes;

    protected AbstractToNumberConverter(ISet<(Type SourceType, Type TargetType)> convertableTypes)
        : base(null)
    {
        this.convertableTypes = convertableTypes;
    }

    public override bool Matches(Type sourceType, Type targetType)
    {
        Type targetTypeCheck = ConversionUtils.GetNullableElementType(targetType);
        return convertableTypes.Contains((sourceType, targetTypeCheck));
    }

    public override object Convert(object source, Type sourceType, Type targetType)
    {
        targetType = ConversionUtils.GetNullableElementType(targetType);

        if (typeof(int) == targetType)
        {
            return System.Convert.ToInt32(source);
        }

        if (typeof(float) == targetType)
        {
            return System.Convert.ToSingle(source);
        }

        if (typeof(uint) == targetType)
        {
            return System.Convert.ToUInt32(source);
        }

        if (typeof(ulong) == targetType)
        {
            return System.Convert.ToUInt64(source);
        }

        if (typeof(long) == targetType)
        {
            return System.Convert.ToInt64(source);
        }

        if (typeof(double) == targetType)
        {
            return System.Convert.ToDouble(source);
        }

        if (typeof(short) == targetType)
        {
            return System.Convert.ToInt16(source);
        }

        if (typeof(ushort) == targetType)
        {
            return System.Convert.ToUInt16(source);
        }

        if (typeof(decimal) == targetType)
        {
            return System.Convert.ToDecimal(source);
        }

        if (typeof(byte) == targetType)
        {
            return System.Convert.ToByte(source);
        }

        if (typeof(sbyte) == targetType)
        {
            return System.Convert.ToSByte(source);
        }

        throw new ArgumentException($"Target type '{targetType.Name}' is not supported.", nameof(targetType));
    }
}
