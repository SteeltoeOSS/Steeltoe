// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Converter
{
    public class ObjectToArrayConverter : AbstractGenericConditionalConverter
    {
        private readonly IConversionService _conversionService;

        public ObjectToArrayConverter(IConversionService conversionService)
            : base(new HashSet<(Type Source, Type Target)> { (typeof(object), typeof(object[])) })
        {
            _conversionService = conversionService;
        }

        public override bool Matches(Type sourceType, Type targetType)
        {
            if (!targetType.IsArray)
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

            var targetElementType = ConversionUtils.GetElementType(targetType);
            if (targetElementType == null)
            {
                throw new InvalidOperationException("No target element type");
            }

            var target = Array.CreateInstance(targetElementType, 1);
            var targetElement = _conversionService.Convert(source, sourceType, targetElementType);
            target.SetValue(targetElement, 0);
            return target;
        }
    }
}
