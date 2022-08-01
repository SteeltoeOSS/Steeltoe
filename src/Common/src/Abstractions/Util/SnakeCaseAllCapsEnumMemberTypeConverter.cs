// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Globalization;

namespace Steeltoe.Common.Util;

/// <summary>
/// Converts between pascal-cased enum members and their snake-case-all-caps string representation.
/// <example>
/// <code><![CDATA[
/// MessageDeliveryMode.NonPersistent <-> NON_PERSISTENT
/// ]]></code>
/// </example>
/// </summary>
/// <typeparam name="TEnum">
/// The enumeration type.
/// </typeparam>
public sealed class SnakeCaseAllCapsEnumMemberTypeConverter<TEnum> : TypeConverter
    where TEnum : struct, Enum
{
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    /// <inheritdoc />
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return destinationType.IsEnum;
    }

    /// <inheritdoc />
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value == null)
        {
            return null;
        }

        string snakeCaseText = (string)value;

        var converter = new SnakeCaseEnumConverter<TEnum>(SnakeCaseStyle.AllCaps);
        string pascalCaseText = converter.ToPascalCase(snakeCaseText);

        return Enum.Parse<TEnum>(pascalCaseText);
    }

    /// <inheritdoc />
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (value == null)
        {
            return null;
        }

        string pascalCaseText = value.ToString();

        var converter = new SnakeCaseEnumConverter<TEnum>(SnakeCaseStyle.AllCaps);
        string snakeCaseText = converter.ToSnakeCase(pascalCaseText);

        return snakeCaseText;
    }
}
