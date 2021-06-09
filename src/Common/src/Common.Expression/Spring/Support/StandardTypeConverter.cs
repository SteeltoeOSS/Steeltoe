// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Converter;
using System;

namespace Steeltoe.Common.Expression.Internal.Spring.Support
{
    public class StandardTypeConverter : ITypeConverter
    {
        public StandardTypeConverter()
        {
            ConversionService = DefaultConversionService.Singleton;
        }

        public StandardTypeConverter(IConversionService conversionService)
        {
            if (conversionService == null)
            {
                throw new ArgumentNullException(nameof(conversionService));
            }

            ConversionService = conversionService;
        }

        public IConversionService ConversionService { get; set; }

        public bool CanConvert(Type sourceType, Type targetType)
        {
            return ConversionService.CanConvert(sourceType, targetType);
        }

        public object ConvertValue(object value, Type sourceType, Type targetType)
        {
            try
            {
                return ConversionService.Convert(value, sourceType, targetType);
            }
            catch (ConversionException ex)
            {
                var message = "null";
                if (sourceType != null)
                {
                    message = sourceType.ToString();
                }
                else if (value != null)
                {
                    message = value.GetType().FullName;
                }

                throw new SpelEvaluationException(ex, SpelMessage.TYPE_CONVERSION_ERROR, message, targetType.ToString());
            }
        }
    }
}
