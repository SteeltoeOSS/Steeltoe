// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Steeltoe.Common.Converter
{
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
                return ConversionUtils.CanConvertElements(
                    ConversionUtils.GetElementType(sourceType), ConversionUtils.GetElementType(targetType), _conversionService);
            }

            return false;
        }

        public override object Convert(object source, Type sourceType, Type targetType)
        {
            if (source is not IEnumerable sourceCollection)
            {
                return null;
            }

            var list = ConversionUtils.CreateCompatListFor(targetType);
            if (list == null)
            {
                throw new InvalidOperationException("Unable to create compatible list");
            }

            if (!targetType.IsGenericType)
            {
                foreach (var elem in sourceCollection)
                {
                    list.Add(elem);
                }
            }
            else
            {
                var targetElementType = ConversionUtils.GetElementType(targetType) ?? typeof(object);
                foreach (var sourceElement in sourceCollection)
                {
                    if (sourceElement != null)
                    {
                        var targetElement = _conversionService.Convert(sourceElement, sourceElement.GetType(), targetElementType);
                        list.Add(targetElement);
                    }
                    else
                    {
                        list.Add(sourceElement);
                    }
                }
            }

            return list;
        }

        private static ISet<(Type Source, Type Target)> GetConvertiblePairs()
        {
            return new HashSet<(Type Source, Type Target)>()
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
}
