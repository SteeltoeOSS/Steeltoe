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
