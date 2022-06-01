// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Steeltoe.Common.Converter;

public class ObjectToCollectionConverter : AbstractToCollectionConverter
{
    public ObjectToCollectionConverter(IConversionService conversionService)
        : base(GetConvertiblePairs(), conversionService)
    {
    }

    public override bool Matches(Type sourceType, Type targetType)
    {
        if (ConversionUtils.CanCreateCompatListFor(targetType))
        {
            return ConversionUtils.CanConvertElements(sourceType, ConversionUtils.GetElementType(targetType), _conversionService);
        }

        return false;
    }

    public override object Convert(object source, Type sourceType, Type targetType)
    {
        if (source == null)
        {
            return null;
        }

        var elementDesc = ConversionUtils.GetElementType(targetType);
        var target = ConversionUtils.CreateCompatListFor(targetType);

        if (elementDesc == null)
        {
            target.Add(source);
        }
        else
        {
            var singleElement = _conversionService.Convert(source, sourceType, elementDesc);
            target.Add(singleElement);
        }

        return target;
    }

    private static ISet<(Type Source, Type Target)> GetConvertiblePairs()
    {
        return new HashSet<(Type Source, Type Target)>
        {
            (typeof(object), typeof(ICollection)),
            (typeof(object), typeof(ICollection<>)),
            (typeof(object), typeof(IEnumerable)),
            (typeof(object), typeof(IEnumerable<>)),
        };
    }
}
