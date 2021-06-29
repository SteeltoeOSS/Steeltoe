// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Invocation;
using System;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Attributes.Support
{
    public class PayloadArgumentResolver : IHandlerMethodArgumentResolver
    {
        private readonly IMessageConverter _converter;
        private readonly bool _useDefaultResolution;

        public PayloadArgumentResolver(IMessageConverter messageConverter)
        : this(messageConverter, true)
        {
        }

        public PayloadArgumentResolver(IMessageConverter messageConverter, bool useDefaultResolution)
        {
            if (messageConverter == null)
            {
                throw new ArgumentNullException(nameof(messageConverter));
            }

            _converter = messageConverter;
            _useDefaultResolution = useDefaultResolution;
        }

        public bool SupportsParameter(ParameterInfo parameter)
        {
            return parameter.GetCustomAttribute<PayloadAttribute>() != null || _useDefaultResolution;
        }

        public object ResolveArgument(ParameterInfo parameter, IMessage message)
        {
            var ann = parameter.GetCustomAttribute<PayloadAttribute>();

            if (ann != null && !string.IsNullOrEmpty(ann.Expression))
            {
                throw new InvalidOperationException("Payload Attribute expressions not supported by this resolver");
            }

            var payload = message.Payload;
            if (IsEmptyPayload(payload))
            {
                if (ann == null || ann.Required)
                {
                    var paramName = GetParameterName(parameter);
                    throw new MethodArgumentNotValidException(message, parameter, "Payload value must not be empty");
                }
                else
                {
                    return null;
                }
            }

            var targetClass = parameter.ParameterType;
            var payloadClass = payload.GetType();
            if (targetClass.IsAssignableFrom(payloadClass))
            {
                return payload;
            }
            else
            {
                if (_converter is ISmartMessageConverter smartConverter)
                {
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

        private string GetParameterName(ParameterInfo param)
        {
            var paramName = param.Name;
            return !string.IsNullOrEmpty(paramName) ? paramName : "Arg " + param.Position;
        }
    }
}
