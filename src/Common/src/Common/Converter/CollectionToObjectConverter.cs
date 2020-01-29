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
    public class CollectionToObjectConverter : AbstractGenericConditionalConverter
    {
        private readonly IConversionService _conversionService;

        public CollectionToObjectConverter(IConversionService conversionService)
            : base(new HashSet<(Type Source, Type Target)>() { (typeof(ICollection), typeof(object)) })
        {
            _conversionService = conversionService;
        }

        public override bool Matches(Type sourceType, Type targetType)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(sourceType))
            {
                return false;
            }

            return ConversionUtils.CanConvertElements(
                    ConversionUtils.GetElementType(sourceType), targetType, _conversionService);
        }

        public override object Convert(object source, Type sourceType, Type targetType)
        {
            if (source == null)
            {
                return null;
            }

            if (targetType.IsAssignableFrom(sourceType))
            {
                return source;
            }

            var sourceCollection = source as ICollection;
            if (sourceCollection.Count == 0)
            {
                return null;
            }

            var enumerator = sourceCollection.GetEnumerator();
            enumerator.MoveNext();

            return _conversionService.Convert(enumerator.Current, enumerator.Current.GetType(), targetType);
        }
    }
}
