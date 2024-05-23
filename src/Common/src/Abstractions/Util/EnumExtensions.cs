// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Util;

public static class EnumExtensions
{
    /// <summary>
    /// Converts a pascal-cased enum member to its snake-case string representation.
    /// </summary>
    /// <typeparam name="TEnum">
    /// The enumeration type.
    /// </typeparam>
    /// <param name="value">
    /// The enumeration member to convert.
    /// </param>
    /// <param name="style">
    /// The output style.
    /// </param>
    public static string ToSnakeCaseString<TEnum>(this TEnum value, SnakeCaseStyle style)
        where TEnum : struct, Enum
    {
        string pascalCaseText = value.ToString();

        var converter = new SnakeCaseEnumConverter<TEnum>(style);
        return converter.ToSnakeCase(pascalCaseText);
    }
}
