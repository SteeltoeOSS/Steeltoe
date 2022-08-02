// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Converter;

namespace Steeltoe.Common.Expression.Internal.Spring.Support;

public class StandardTypeConverter : ITypeConverter
{
    public IConversionService ConversionService { get; set; }

    public StandardTypeConverter()
    {
        ConversionService = DefaultConversionService.Singleton;
    }

    public StandardTypeConverter(IConversionService conversionService)
    {
        ConversionService = conversionService ?? throw new ArgumentNullException(nameof(conversionService));
    }

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
            string message = "null";

            if (sourceType != null)
            {
                message = sourceType.ToString();
            }
            else if (value != null)
            {
                message = value.GetType().FullName;
            }

            throw new SpelEvaluationException(ex, SpelMessage.TypeConversionError, message, targetType.ToString());
        }
    }
}
