// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Invocation;
using Steeltoe.Messaging.Support;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Attributes.Support;

public class MessageMethodArgumentResolver : IHandlerMethodArgumentResolver
{
    protected readonly IMessageConverter Converter;

    public MessageMethodArgumentResolver()
        : this(null)
    {
    }

    public MessageMethodArgumentResolver(IMessageConverter converter) => Converter = converter;

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
                $"Cannot convert from actual payload type '{payloadType}' to expected payload type '{targetPayloadType}' when payload is empty");
        }

        payload = ConvertPayload(message, parameter, targetPayloadType);
        return MessageBuilder.CreateMessage(payload, message.Headers, targetPayloadType);
    }

    public virtual bool SupportsParameter(ParameterInfo parameter) => typeof(IMessage).IsAssignableFrom(parameter.ParameterType);

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
        return payload switch
        {
            null => true,
            byte[] bytes => bytes.Length == 0,
            string sPayload => string.IsNullOrEmpty(sPayload),
            _ => false
        };
    }

    private object ConvertPayload(IMessage message, ParameterInfo parameter, Type targetPayloadType)
    {
        object result = null;
        if (Converter is ISmartMessageConverter smartConverter)
        {
            result = smartConverter.FromMessage(message, targetPayloadType, parameter);
        }
        else if (Converter != null)
        {
            result = Converter.FromMessage(message, targetPayloadType);
        }

        if (result == null)
        {
            var payloadType = message.Payload != null ? message.Payload.GetType().ToString() : "null";
            throw new MessageConversionException(
                message,
                $"No converter found from actual payload type '{payloadType}' to expected payload type '{targetPayloadType}'");
        }

        return result;
    }
}
