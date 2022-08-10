// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Converter;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Messaging;

namespace Steeltoe.Integration.Util;

public class ServiceFactoryTypeConverter : ITypeConverter
{
    public IConversionService ConversionService { get; set; }

    public ServiceFactoryTypeConverter()
    {
        ConversionService = DefaultConversionService.Singleton;
    }

    public ServiceFactoryTypeConverter(IConversionService conversionService)
    {
        ConversionService = conversionService;
    }

    public bool CanConvert(Type sourceType, Type targetType)
    {
        if (ConversionService.CanConvert(sourceType, targetType))
        {
            return true;
        }

        return false;
    }

    public object ConvertValue(object value, Type sourceType, Type targetType)
    {
        if (targetType == typeof(void) && value == null)
        {
            return null;
        }

        if (sourceType != null && ((sourceType == typeof(IMessageHeaders) && targetType == typeof(IMessageHeaders)) ||
            (targetType.IsAssignableFrom(sourceType) && sourceType.IsArray && sourceType.IsPrimitive)))
        {
            return value;
        }

        return ConversionService.Convert(value, sourceType, targetType);
    }
}
