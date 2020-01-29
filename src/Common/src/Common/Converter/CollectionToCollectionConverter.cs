// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
            // ICollection sourceCollection = source as ICollection;
            var sourceCollection = source as IEnumerable;
            if (sourceCollection == null)
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
