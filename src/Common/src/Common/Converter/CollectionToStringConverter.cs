// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Steeltoe.Common.Converter;

public class CollectionToStringConverter : AbstractGenericConditionalConverter
{
    private readonly IConversionService _conversionService;

    public CollectionToStringConverter(IConversionService conversionService)
        : base(new HashSet<(Type Source, Type Target)>() { (typeof(ICollection), typeof(string)) })
    {
        _conversionService = conversionService;
    }

    public override bool Matches(Type sourceType, Type targetType)
    {
        return ConversionUtils.CanConvertElements(
            ConversionUtils.GetElementType(sourceType), targetType, _conversionService);
    }

    public override object Convert(object source, Type sourceType, Type targetType)
    {
        if (source == null)
        {
            return null;
        }

        var sourceCollection = (ICollection)source;
        if (sourceCollection.Count == 0)
        {
            return string.Empty;
        }

        return ConversionUtils.ToString(sourceCollection, targetType, _conversionService);
    }
}