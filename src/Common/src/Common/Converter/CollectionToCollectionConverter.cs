// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace Steeltoe.Common.Converter;

public class CollectionToCollectionConverter : AbstractToCollectionConverter
{
    public CollectionToCollectionConverter(IConversionService conversionService)
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

        if (ConversionUtils.CanCreateCompatListFor(targetType))
        {
            return ConversionUtils.CanConvertElements(ConversionUtils.GetElementType(sourceType), ConversionUtils.GetElementType(targetType),
                ConversionService);
        }

        return false;
    }

    public override object Convert(object source, Type sourceType, Type targetType)
    {
        if (source is not IEnumerable sourceCollection)
        {
            return null;
        }

        IList list = ConversionUtils.CreateCompatListFor(targetType);

        if (list == null)
        {
            throw new InvalidOperationException("Unable to create compatible list");
        }

        if (!targetType.IsGenericType)
        {
            foreach (object elem in sourceCollection)
            {
                list.Add(elem);
            }
        }
        else
        {
            Type targetElementType = ConversionUtils.GetElementType(targetType) ?? typeof(object);

            foreach (object sourceElement in sourceCollection)
            {
                if (sourceElement != null)
                {
                    object targetElement = ConversionService.Convert(sourceElement, sourceElement.GetType(), targetElementType);
                    list.Add(targetElement);
                }
                else
                {
                    list.Add(null);
                }
            }
        }

        return list;
    }

    private static ISet<(Type SourceType, Type TargetType)> GetConvertiblePairs()
    {
        return new HashSet<(Type SourceType, Type TargetType)>
        {
            (typeof(ICollection), typeof(ICollection)),
            (typeof(ICollection), typeof(ICollection<>)),
            (typeof(ICollection), typeof(IEnumerable<>)),
            (typeof(ICollection), typeof(IEnumerable)),
            (typeof(ISet<>), typeof(ICollection)),
            (typeof(ISet<>), typeof(ICollection<>)),
            (typeof(ISet<>), typeof(IEnumerable<>)),
            (typeof(ISet<>), typeof(IEnumerable))
        };
    }
}
