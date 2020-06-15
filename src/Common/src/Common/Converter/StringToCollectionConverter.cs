// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
