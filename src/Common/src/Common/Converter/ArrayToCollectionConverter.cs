// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Steeltoe.Common.Converter;

public class ArrayToCollectionConverter : AbstractToCollectionConverter
{
    public ArrayToCollectionConverter(IConversionService conversionService)
        : base(GetConvertiblePairs(), conversionService)
    {
    }

    public override bool Matches(Type sourceType, Type targetType)
    {
        // NO OP check Arrays already implement IList, etc.
        if (targetType.IsAssignableFrom(sourceType))
        {
            return false;
        }

        if (sourceType.IsArray && ConversionUtils.CanCreateCompatListFor(targetType))
        {
            return ConversionUtils.CanConvertElements(
                ConversionUtils.GetElementType(sourceType), ConversionUtils.GetElementType(targetType), _conversionService);
        }

        return false;
    }

    public override object Convert(object source, Type sourceType, Type targetType)
    {
        if (source is not Array asArray)
        {
            return null;
        }

        var len = asArray.GetLength(0);
        var arrayElementType = ConversionUtils.GetElementType(asArray.GetType());

        var list = ConversionUtils.CreateCompatListFor(targetType);
        if (list == null)
        {
            throw new InvalidOperationException("Unable to create compatible list");
        }

        if (!targetType.IsGenericType)
        {
            for (var i = 0; i < len; i++)
            {
                list.Add(asArray.GetValue(i));
            }
        }
        else
        {
            var targetElementType = ConversionUtils.GetElementType(targetType);
            for (var i = 0; i < len; i++)
            {
                var sourceElement = asArray.GetValue(i);
                var targetElement = _conversionService.Convert(sourceElement, arrayElementType, targetElementType);
                list.Add(targetElement);
            }
        }

        return list;
    }

    private static ISet<(Type Source, Type Target)> GetConvertiblePairs()
    {
        return new HashSet<(Type Source, Type Target)>
        {
            (typeof(object[]), typeof(ICollection)),
            (typeof(object[]), typeof(ICollection<>)),
            (typeof(object[]), typeof(IEnumerable)),
            (typeof(object[]), typeof(IEnumerable<>)),
        };
    }
}
