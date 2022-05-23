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
            ConversionService = conversionService ?? throw new ArgumentNullException(nameof(conversionService));
        }

        public IConversionService ConversionService { get; set; }

        public bool CanConvert(Type source, Type target)
        {
            return ConversionService.CanConvert(source, target);
        }

        public object ConvertValue(object value, Type source, Type target)
        {
            try
            {
                return ConversionService.Convert(value, source, target);
            }
            catch (ConversionException ex)
            {
                var message = "null";
                if (source != null)
                {
                    message = source.ToString();
                }
                else if (value != null)
                {
                    message = value.GetType().FullName;
                }

                throw new SpelEvaluationException(ex, SpelMessage.TYPE_CONVERSION_ERROR, message, target.ToString());
            }
        }
    }
}
