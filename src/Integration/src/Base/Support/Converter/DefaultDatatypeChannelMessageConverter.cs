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
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using System;

namespace Steeltoe.Integration.Support.Converter
{
    public class DefaultDatatypeChannelMessageConverter : IMessageConverter
    {
        private readonly IConversionService _conversionService;

        public DefaultDatatypeChannelMessageConverter(IConversionService conversionService = null)
        {
            _conversionService = conversionService ?? DefaultConversionService.Singleton;
        }

        public object FromMessage(IMessage message, Type targetClass)
        {
            if (_conversionService.CanConvert(message.Payload.GetType(), targetClass))
            {
                return _conversionService.Convert(message.Payload, message.Payload.GetType(), targetClass);
            }
            else
            {
                return null;
            }
        }

        public T FromMessage<T>(IMessage message)
        {
            return (T)FromMessage(message, typeof(T));
        }

        public IMessage ToMessage(object payload, IMessageHeaders headers = null)
        {
            throw new NotImplementedException("This converter does not support this method");
        }
    }
}
