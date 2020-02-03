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

using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Invocation;
using System;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Attributes.Support
{
    public class PayloadMethodArgumentResolver : IHandlerMethodArgumentResolver
    {
        protected readonly IMessageConverter _converter;
        protected readonly bool _useDefaultResolution;

        public PayloadMethodArgumentResolver(IMessageConverter messageConverter)
        : this(messageConverter, true)
        {
        }

        public PayloadMethodArgumentResolver(IMessageConverter messageConverter, bool useDefaultResolution)
        {
            if (messageConverter == null)
            {
                throw new ArgumentNullException(nameof(messageConverter));
            }

            _converter = messageConverter;
            _useDefaultResolution = useDefaultResolution;
        }

        public virtual bool SupportsParameter(ParameterInfo parameter)
        {
            return parameter.GetCustomAttribute<PayloadAttribute>() != null || _useDefaultResolution;
        }

        public virtual object ResolveArgument(ParameterInfo parameter, IMessage message)
        {
            var ann = parameter.GetCustomAttribute<PayloadAttribute>();

            if (ann != null && !string.IsNullOrEmpty(ann.Expression))
            {
                throw new InvalidOperationException("Payload expressions not supported by this resolver");
            }

            var payload = message.Payload;
            if (IsEmptyPayload(payload))
            {
                if (ann == null || ann.Required)
                {
                    var paramName = GetParameterName(parameter);
                    throw new MethodArgumentNotValidException(message, parameter, "Payload value must not be empty when binding to: " + paramName);
                }
                else
                {
                    return null;
                }
            }

            var targetClass = ResolveTargetClass(parameter, message);
            var payloadClass = payload.GetType();
            if (targetClass.IsAssignableFrom(payloadClass))
            {
                Validate(message, parameter, payload);
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

                Validate(message, parameter, payload);

                return payload;
            }
        }

        protected virtual Type ResolveTargetClass(ParameterInfo parameter, IMessage message)
        {
            return parameter.ParameterType;
        }

        protected virtual bool IsEmptyPayload(object payload)
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

        protected virtual void Validate(IMessage message, ParameterInfo parameter, object target)
        {
        }

        private string GetParameterName(ParameterInfo param)
        {
            var paramName = param.Name;
            return paramName ?? "Arg " + param.Position;
        }
    }
}
