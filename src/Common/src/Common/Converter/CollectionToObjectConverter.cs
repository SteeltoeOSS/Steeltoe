// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Steeltoe.Common.Converter;

public class CollectionToObjectConverter : AbstractGenericConditionalConverter
{
    private readonly IConversionService _conversionService;

    public CollectionToObjectConverter(IConversionService conversionService, ISet<(Type source, Type target)> convertableTypes = null)
        : base(convertableTypes ?? new HashSet<(Type Source, Type Target)> { (typeof(ICollection), typeof(object)) })
    {
        _conversionService = conversionService;
    }

    public override bool Matches(Type sourceType, Type targetType)
    {
        if (!typeof(IEnumerable).IsAssignableFrom(sourceType))
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

        var sourceCollection = source as ICollection;
        if (sourceCollection.Count == 0)
        {
            return null;
        }

        var enumerator = sourceCollection.GetEnumerator();
        enumerator.MoveNext();

        return _conversionService.Convert(enumerator.Current, enumerator.Current.GetType(), targetType);
    }
}
