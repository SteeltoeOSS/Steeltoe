// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;

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
            return System.Convert.ToInt32(source, CultureInfo.InvariantCulture);
        }

        if (typeof(float) == targetType)
        {
            return System.Convert.ToSingle(source, CultureInfo.InvariantCulture);
        }

        if (typeof(uint) == targetType)
        {
            return System.Convert.ToUInt32(source, CultureInfo.InvariantCulture);
        }

        if (typeof(ulong) == targetType)
        {
            return System.Convert.ToUInt64(source, CultureInfo.InvariantCulture);
        }

        if (typeof(long) == targetType)
        {
            return System.Convert.ToInt64(source, CultureInfo.InvariantCulture);
        }

        if (typeof(double) == targetType)
        {
            return System.Convert.ToDouble(source, CultureInfo.InvariantCulture);
        }

        if (typeof(short) == targetType)
        {
            return System.Convert.ToInt16(source, CultureInfo.InvariantCulture);
        }

        if (typeof(ushort) == targetType)
        {
            return System.Convert.ToUInt16(source, CultureInfo.InvariantCulture);
        }

        if (typeof(decimal) == targetType)
        {
            return System.Convert.ToDecimal(source, CultureInfo.InvariantCulture);
        }

        if (typeof(byte) == targetType)
        {
            return System.Convert.ToByte(source, CultureInfo.InvariantCulture);
        }

        if (typeof(sbyte) == targetType)
        {
            return System.Convert.ToSByte(source, CultureInfo.InvariantCulture);
        }

        throw new ArgumentException($"Target type '{targetType.Name}' is not supported.", nameof(targetType));
    }
}
