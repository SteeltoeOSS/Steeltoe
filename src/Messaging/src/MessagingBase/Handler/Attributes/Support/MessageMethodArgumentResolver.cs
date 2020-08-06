﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Invocation;
using Steeltoe.Messaging.Support;
using System;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Attributes.Support
{
    public class MessageMethodArgumentResolver : IHandlerMethodArgumentResolver
    {
        protected readonly IMessageConverter _converter;

        public MessageMethodArgumentResolver()
            : this(null)
        {
        }

        public MessageMethodArgumentResolver(IMessageConverter converter)
        {
            _converter = converter;
        }

        public virtual object ResolveArgument(ParameterInfo parameter, IMessage message)
        {
            var targetPayloadType = GetPayloadType(parameter, message);

            var payload = message.Payload;
            if (targetPayloadType.IsInstanceOfType(payload))
            {
                return message;
            }

            if (IsEmptyPayload(payload))
            {
                var payloadType = payload != null ? payload.GetType().ToString() : "null";

                throw new MessageConversionException(
                    message,
                    "Cannot convert from actual payload type '" + payloadType + "' to expected payload type '" + targetPayloadType.ToString() + "' when payload is empty");
            }

            payload = ConvertPayload(message, parameter, targetPayloadType);
            return MessageBuilder.CreateMessage(payload, message.Headers, targetPayloadType);
        }

        public virtual bool SupportsParameter(ParameterInfo parameter)
        {
            return typeof(IMessage).IsAssignableFrom(parameter.ParameterType);
        }

        protected virtual Type GetPayloadType(ParameterInfo parameter, IMessage message)
        {
            if (parameter.ParameterType.IsGenericType)
            {
                if (parameter.ParameterType.IsConstructedGenericType)
                {
                    return parameter.ParameterType.GenericTypeArguments[0];
                }

                var method = parameter.Member as MethodInfo;
                Type[] methodArgs = null;
                Type[] typeArgs = null;
                if (method.IsGenericMethod)
                {
                    methodArgs = method.GetGenericArguments();
                }

                var type = method.DeclaringType;
                if (type.IsGenericType)
                {
                    typeArgs = type.GetGenericArguments();
                }

                return type.Module.ResolveType(parameter.ParameterType.MetadataToken, typeArgs, methodArgs);
            }

            return typeof(object);
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

        private object ConvertPayload(IMessage message, ParameterInfo parameter, Type targetPayloadType)
        {
            object result = null;
            if (_converter is ISmartMessageConverter smartConverter)
            {
                result = smartConverter.FromMessage(message, targetPayloadType, parameter);
            }
            else if (_converter != null)
            {
                result = _converter.FromMessage(message, targetPayloadType);
            }

            if (result == null)
            {
                var payloadType = message.Payload != null ? message.Payload.GetType().ToString() : "null";
                throw new MessageConversionException(
                    message,
                    "No converter found from actual payload type '" + payloadType + "' to expected payload type '" + targetPayloadType.ToString() + "'");
            }

            return result;
        }
    }
}
