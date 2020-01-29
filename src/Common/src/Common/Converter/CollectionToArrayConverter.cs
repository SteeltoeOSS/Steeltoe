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
    public class CollectionToArrayConverter : AbstractGenericConditionalConverter
    {
        private readonly IConversionService _conversionService;

        public CollectionToArrayConverter(IConversionService conversionService)
            : base(GetConvertiblePairs())
        {
            _conversionService = conversionService;
        }

        public override bool Matches(Type sourceType, Type targetType)
        {
            if (!targetType.IsArray || sourceType.IsArray)
            {
                return false;
            }

            return ConversionUtils.CanConvertElements(
                    ConversionUtils.GetElementType(sourceType), ConversionUtils.GetElementType(targetType), _conversionService);
        }

        public override object Convert(object source, Type sourceType, Type targetType)
        {
            if (source == null)
            {
                return null;
            }

            var targetElementType = ConversionUtils.GetElementType(targetType);
            if (targetElementType == null)
            {
                throw new InvalidOperationException("No target element type");
            }

            var enumerable = source as IEnumerable;
            var len = ConversionUtils.Count(enumerable);

            var array = Array.CreateInstance(targetElementType, len);
            var i = 0;
            foreach (var sourceElement in enumerable)
            {
                var targetElement = _conversionService.Convert(sourceElement, sourceElement.GetType(), targetElementType);
                array.SetValue(targetElement, i++);
            }

            return array;
        }

        private static ISet<(Type Source, Type Target)> GetConvertiblePairs()
        {
            return new HashSet<(Type Source, Type Target)>()
            {
                (typeof(ICollection), typeof(object[])),
                (typeof(ISet<>), typeof(object[])),
            };
        }
    }
}
