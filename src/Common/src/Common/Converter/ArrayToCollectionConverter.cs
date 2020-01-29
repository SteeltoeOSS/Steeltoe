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
            var asArray = source as Array;

            if (asArray == null)
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
            return new HashSet<(Type Source, Type Target)>()
            {
                (typeof(object[]), typeof(ICollection)),
                (typeof(object[]), typeof(ICollection<>)),
                (typeof(object[]), typeof(IEnumerable)),
                (typeof(object[]), typeof(IEnumerable<>)),
            };
        }
    }
}
