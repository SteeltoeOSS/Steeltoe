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
    public class StringToCollectionConverter : AbstractToCollectionConverter
    {
        private readonly char[] _delimit = new char[] { ',' };

        public StringToCollectionConverter(IConversionService conversionService)
            : base(GetConvertiblePairs(), conversionService)
        {
        }

        public override bool Matches(Type sourceType, Type targetType)
        {
            if (sourceType == typeof(string) && ConversionUtils.CanCreateCompatListFor(targetType))
            {
                return ConversionUtils.CanConvertElements(sourceType, ConversionUtils.GetElementType(targetType), _conversionService);
            }

            return false;
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

            var list = ConversionUtils.CreateCompatListFor(targetType);
            if (list == null)
            {
                throw new InvalidOperationException("Unable to create compatable list");
            }

            for (var i = 0; i < fields.Length; i++)
            {
                var sourceElement = fields[i];
                var targetElement = _conversionService.Convert(sourceElement.Trim(), sourceType, targetElementType);
                list.Add(targetElement);
            }

            return list;
        }

        private static ISet<(Type Source, Type Target)> GetConvertiblePairs()
        {
            return new HashSet<(Type Source, Type Target)>()
            {
                (typeof(string), typeof(ICollection)),
                (typeof(string), typeof(ICollection<>)),
                (typeof(string), typeof(IEnumerable)),
                (typeof(string), typeof(IEnumerable<>)),
            };
        }
    }
}
