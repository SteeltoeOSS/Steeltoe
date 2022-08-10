// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;

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
            return ConversionUtils.CanConvertElements(ConversionUtils.GetElementType(sourceType), ConversionUtils.GetElementType(targetType),
                ConversionService);
        }

        return false;
    }

    public override object Convert(object source, Type sourceType, Type targetType)
    {
        if (source is not Array asArray)
        {
            return null;
        }

        int len = asArray.GetLength(0);
        Type arrayElementType = ConversionUtils.GetElementType(asArray.GetType());

        IList list = ConversionUtils.CreateCompatListFor(targetType);

        if (list == null)
        {
            throw new InvalidOperationException("Unable to create compatible list");
        }

        if (!targetType.IsGenericType)
        {
            for (int i = 0; i < len; i++)
            {
                list.Add(asArray.GetValue(i));
            }
        }
        else
        {
            Type targetElementType = ConversionUtils.GetElementType(targetType);

            for (int i = 0; i < len; i++)
            {
                object sourceElement = asArray.GetValue(i);
                object targetElement = ConversionService.Convert(sourceElement, arrayElementType, targetElementType);
                list.Add(targetElement);
            }
        }

        return list;
    }

    private static ISet<(Type SourceType, Type TargetType)> GetConvertiblePairs()
    {
        return new HashSet<(Type SourceType, Type TargetType)>
        {
            (typeof(object[]), typeof(ICollection)),
            (typeof(object[]), typeof(ICollection<>)),
            (typeof(object[]), typeof(IEnumerable)),
            (typeof(object[]), typeof(IEnumerable<>))
        };
    }
}
