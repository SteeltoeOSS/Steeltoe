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
    public class ObjectToCollectionConverter : AbstractToCollectionConverter
    {
        public ObjectToCollectionConverter(IConversionService conversionService)
            : base(GetConvertiblePairs(), conversionService)
        {
        }

        public override bool Matches(Type sourceType, Type targetType)
        {
            if (ConversionUtils.CanCreateCompatListFor(targetType))
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

            var elementDesc = ConversionUtils.GetElementType(targetType);
            var target = ConversionUtils.CreateCompatListFor(targetType);

            if (elementDesc == null)
            {
                target.Add(source);
            }
            else
            {
                var singleElement = _conversionService.Convert(source, sourceType, elementDesc);
                target.Add(singleElement);
            }

            return target;
        }

        private static ISet<(Type Source, Type Target)> GetConvertiblePairs()
        {
            return new HashSet<(Type Source, Type Target)>()
            {
                (typeof(object), typeof(ICollection)),
                (typeof(object), typeof(ICollection<>)),
                (typeof(object), typeof(IEnumerable)),
                (typeof(object), typeof(IEnumerable<>)),
            };
        }
    }
}
