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
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Messaging.Handler.Attributes.Support;
using System;
using System.Reflection;

namespace Steeltoe.Stream.Config
{
    public class SmartPayloadArgumentResolver : PayloadMethodArgumentResolver
    {
        public SmartPayloadArgumentResolver(IMessageConverter messageConverter)
           : base(messageConverter)
        {
        }

        public SmartPayloadArgumentResolver(IMessageConverter messageConverter, bool useDefaultResolution)
        : base(messageConverter, useDefaultResolution)
        {
        }

        public override bool SupportsParameter(ParameterInfo parameter)
        {
            return !typeof(IMessage).IsAssignableFrom(parameter.ParameterType)
                && !typeof(MessageHeaders).IsAssignableFrom(parameter.ParameterType)
                && parameter.GetCustomAttribute<HeaderAttribute>() == null
                && parameter.GetCustomAttribute<HeadersAttribute>() == null;
        }

        public override object ResolveArgument(ParameterInfo parameter, IMessage message)
        {
            var ann = parameter.GetCustomAttribute<PayloadAttribute>();
            if (ann != null && !string.IsNullOrEmpty(ann.Expression))
            {
                throw new InvalidOperationException("PayloadAttribute SpEL expressions not supported by this resolver");
            }

            var payload = message.Payload;
            if (IsEmptyPayload(payload))
            {
                if (ann == null || ann.Required)
                {
                    throw new MethodArgumentNotValidException(message, parameter, "Payload value must not be empty");
                }
                else
                {
                    return null;
                }
            }

            var targetClass = parameter.ParameterType;
            var payloadClass = payload.GetType();
            if (ConversionNotRequired(payloadClass, targetClass))
            {
                return payload;
            }
            else
            {
                if (_converter is ISmartMessageConverter)
                {
                    var smartConverter = (ISmartMessageConverter)_converter;
                    payload = smartConverter.FromMessage(message, targetClass, parameter);
                }
                else
                {
                    payload = _converter.FromMessage(message, targetClass);
                }

                if (payload == null)
                {
                    throw new MessageConversionException(message, "Cannot convert from [" + payloadClass.Name + "] to [" + targetClass.Name + "] for " + message);
                }

                return payload;
            }
        }

        private bool ConversionNotRequired(Type a, Type b)
        {
            return b == typeof(object) ? ClassUtils.IsAssignable(a, b) : ClassUtils.IsAssignable(b, a);
        }
    }
}
