// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Converter;
using System;

namespace Steeltoe.Messaging.Converter
{
    public class GenericMessageConverter : SimpleMessageConverter
    {
        private readonly IConversionService conversionService;

        public GenericMessageConverter()
        {
            conversionService = DefaultConversionService.Singleton;
        }

        public GenericMessageConverter(IConversionService conversionService)
        {
            if (conversionService == null)
            {
                throw new ArgumentNullException(nameof(conversionService));
            }

            this.conversionService = conversionService;
        }

        public override object FromMessage(IMessage message, Type targetClass)
        {
            var payload = message.Payload;
            if (conversionService.CanConvert(payload.GetType(), targetClass))
            {
                try
                {
                    return conversionService.Convert(payload, payload.GetType(), targetClass);
                }
                catch (ConversionException ex)
                {
                    throw new MessageConversionException(message, "Failed to convert message payload '" + payload + "' to '" + targetClass.Name + "'", ex);
                }
            }

            return targetClass.IsInstanceOfType(payload) ? payload : null;
        }
    }
}
