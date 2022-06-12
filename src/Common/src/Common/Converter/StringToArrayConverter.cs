// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Converter;

public class StringToArrayConverter : AbstractGenericConditionalConverter
{
    private readonly char[] _delimit = { ',' };

    private readonly IConversionService _conversionService;

    public StringToArrayConverter(IConversionService conversionService)
        : base(new HashSet<(Type Source, Type Target)> { (typeof(string), typeof(object[])) })
    {
        _conversionService = conversionService;
    }

    public override bool Matches(Type sourceType, Type targetType)
    {
        if (sourceType != typeof(string) || !targetType.IsArray)
        {
            return false;
        }

        return ConversionUtils.CanConvertElements(sourceType, ConversionUtils.GetElementType(targetType), _conversionService);
    }

    public override object Convert(object source, Type sourceType, Type targetType)
    {
        if (source == null)
        {
            return null;
        }

        var sourceString = source as string;
        var fields = sourceString.Split(_delimit, StringSplitOptions.RemoveEmptyEntries);
        var targetElementType = ConversionUtils.GetElementType(targetType);
        if (targetElementType == null)
        {
            throw new InvalidOperationException("No target element type");
        }

        // Handle string, not delimited, to char[]
        if (fields.Length == 1 &&
            sourceString[sourceString.Length - 1] != _delimit[0] &&
            targetElementType == typeof(char))
        {
            return sourceString.ToCharArray();
        }

        var target = Array.CreateInstance(targetElementType, fields.Length);
        for (var i = 0; i < fields.Length; i++)
        {
            var sourceElement = fields[i];
            var targetElement = _conversionService.Convert(sourceElement.Trim(), sourceType, targetElementType);
            target.SetValue(targetElement, i);
        }

        return target;
    }
}
