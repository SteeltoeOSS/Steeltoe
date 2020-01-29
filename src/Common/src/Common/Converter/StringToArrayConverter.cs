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
    public class StringToArrayConverter : AbstractGenericConditionalConverter
    {
        private readonly char[] _delimit = new char[] { ',' };

        private readonly IConversionService _conversionService;

        public StringToArrayConverter(IConversionService conversionService)
            : base(new HashSet<(Type Source, Type Target)>() { (typeof(string), typeof(object[])) })
        {
            _conversionService = conversionService;
        }

        public override bool Matches(Type sourceType, Type targetType)
        {
            if (sourceType != typeof(string) || !targetType.IsArray)
            {
                return false;
            }

            return ConversionUtils.CanConvertElements(sourceType, ConversionUtils.GetElementType(targetType), _conversionService);
        }

        public override object Convert(object source, Type sourceType, Type targetType)
        {
            if (source == null)
            {
                return null;
            }

            var sourceString = source as string;
            var fields = sourceString.Split(_delimit, StringSplitOptions.RemoveEmptyEntries);
            var targetElementType = ConversionUtils.GetElementType(targetType);
            if (targetElementType == null)
            {
                throw new InvalidOperationException("No target element type");
            }

            var target = Array.CreateInstance(targetElementType, fields.Length);
            for (var i = 0; i < fields.Length; i++)
            {
                var sourceElement = fields[i];
                var targetElement = _conversionService.Convert(sourceElement.Trim(), sourceType, targetElementType);
                target.SetValue(targetElement, i);
            }

            return target;
        }
    }
}