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

using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Messaging.Support;
using System;
using System.Reflection;

namespace Steeltoe.Stream.Config
{
    public class SmartMessageMethodArgumentResolver : MessageMethodArgumentResolver
    {
        public SmartMessageMethodArgumentResolver()
        : this(null)
        {
        }

        public SmartMessageMethodArgumentResolver(IMessageConverter converter)
        : base(converter)
        {
        }

        public override object ResolveArgument(ParameterInfo parameter, IMessage message)
        {
            var targetPayloadType = GetPayloadType(parameter, message);

            var payloadClass = message.Payload.GetType();

            if (message is ErrorMessage || ConversionNotRequired(payloadClass, targetPayloadType))
            {
                return message;
            }

            var payload = message.Payload;
            if (IsEmptyPayload(payload))
            {
                throw new MessageConversionException(message, "Cannot convert from actual payload type '" + payload.GetType() + "' to expected payload type '" + targetPayloadType + "' when payload is empty");
            }

            payload = ConvertPayload(message, parameter, targetPayloadType);
            return MessageBuilder.CreateMessage(payload, message.Headers);
        }

        protected override bool IsEmptyPayload(object payload)
        {
            if (payload == null)
            {
                return true;
            }
            else if (payload is byte[])
            {
                return ((byte[])payload).Length == 0;
            }
            else if (payload is string)
            {
                return string.IsNullOrEmpty((string)payload);
            }
            else
            {
                return false;
            }
        }

        private bool ConversionNotRequired(Type a, Type b)
        {
            return b == typeof(object) ? ClassUtils.IsAssignable(a, b) : ClassUtils.IsAssignable(b, a);
        }

        private object ConvertPayload(IMessage message, ParameterInfo parameter, Type targetPayloadType)
        {
            object result = null;
            if (_converter is ISmartMessageConverter)
            {
                var smartConverter = (ISmartMessageConverter)_converter;
                result = smartConverter.FromMessage(message, targetPayloadType, parameter);
            }
            else if (_converter != null)
            {
                result = _converter.FromMessage(message, targetPayloadType);
            }

            if (result == null)
            {
                throw new MessageConversionException(message, "No converter found from actual payload type '" + message.Payload.GetType() + "' to expected payload type '" + targetPayloadType + "'");
            }

            return result;
        }
    }
}
