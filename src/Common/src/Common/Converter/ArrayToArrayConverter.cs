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
using System.Collections.Generic;

namespace Steeltoe.Common.Converter
{
    public class ArrayToArrayConverter : AbstractGenericConditionalConverter
    {
        private readonly IConversionService _conversionService;

        public ArrayToArrayConverter(IConversionService conversionService)
            : base(new HashSet<(Type Source, Type Target)>() { (typeof(object[]), typeof(object[])) })
        {
            _conversionService = conversionService;
        }

        public override bool Matches(Type sourceType, Type targetType)
        {
            if (!sourceType.IsArray || !targetType.IsArray)
            {
                return false;
            }

            return ConversionUtils.CanConvertElements(
                    ConversionUtils.GetElementType(sourceType), ConversionUtils.GetElementType(targetType), _conversionService);
        }

        public override object Convert(object source, Type sourceType, Type targetType)
        {
            var targetElement = ConversionUtils.GetElementType(targetType);
            var sourceElement = ConversionUtils.GetElementType(sourceType);
            if (targetElement != null && _conversionService.CanBypassConvert(sourceElement, targetElement))
            {
                return source;
            }

            var sourceArray = source as Array;
            var targetArray = Array.CreateInstance(targetElement, sourceArray.GetLength(0));
            var i = 0;
            foreach (var elem in sourceArray)
            {
                var newElem = _conversionService.Convert(elem, sourceElement, targetElement);
                targetArray.SetValue(newElem, i++);
            }

            return targetArray;
        }
    }
}
