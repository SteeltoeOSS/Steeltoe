// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Converter;

public class ArrayToStringConverter : AbstractGenericConditionalConverter
{
    private readonly IConversionService _conversionService;

    public ArrayToStringConverter(IConversionService conversionService)
        : base(new HashSet<(Type Source, Type Target)> { (typeof(object[]), typeof(string)) })
    {
        _conversionService = conversionService;
    }

    public override bool Matches(Type sourceType, Type targetType)
    {
        if (!sourceType.IsArray || typeof(string) != targetType)
        {
            return false;
        }

        return ConversionUtils.CanConvertElements(
            ConversionUtils.GetElementType(sourceType), targetType, _conversionService);
    }

    public override object Convert(object source, Type sourceType, Type targetType)
    {
        if (source == null)
        {
            return null;
        }

        var sourceArray = source as Array;
        if (sourceArray.GetLength(0) == 0)
        {
            return string.Empty;
        }

        return ConversionUtils.ToString(sourceArray, targetType, _conversionService);
    }
}
