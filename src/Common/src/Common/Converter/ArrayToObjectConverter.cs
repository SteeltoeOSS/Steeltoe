// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Converter;

public class ArrayToObjectConverter : AbstractGenericConditionalConverter
{
    private readonly IConversionService _conversionService;

    public ArrayToObjectConverter(IConversionService conversionService)
        : base(new HashSet<(Type Source, Type Target)>() { (typeof(object[]), typeof(object)) })
    {
        _conversionService = conversionService;
    }

    public override bool Matches(Type sourceType, Type targetType)
    {
        if (!sourceType.IsArray)
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

        if (targetType.IsAssignableFrom(sourceType))
        {
            return source;
        }

        var sourceArray = source as Array;
        if (sourceArray.GetLength(0) == 0)
        {
            return null;
        }

        var firstElement = sourceArray.GetValue(0);
        return _conversionService.Convert(firstElement, firstElement.GetType(), targetType);
    }
}