// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Converter;

public class StringToBooleanConverter : AbstractGenericConditionalConverter
{
    private static readonly ISet<string> TrueValues = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
    {
        "true",
        "on",
        "yes",
        "1"
    };

    private static readonly ISet<string> FalseValues = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
    {
        "false",
        "off",
        "no",
        "0"
    };

    public StringToBooleanConverter()
        : base(null)
    {
    }

    public override bool Matches(Type sourceType, Type targetType)
    {
        Type targetCheck = ConversionUtils.GetNullableElementType(targetType);
        return sourceType == typeof(string) && targetCheck == typeof(bool);
    }

    public override object Convert(object source, Type sourceType, Type targetType)
    {
        if (source == null)
        {
            return null;
        }

        string value = ((string)source).Trim();

        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (TrueValues.Contains(value))
        {
            return true;
        }

        if (FalseValues.Contains(value))
        {
            return false;
        }

        throw new ArgumentException($"Invalid boolean value '{source}'");
    }
}
