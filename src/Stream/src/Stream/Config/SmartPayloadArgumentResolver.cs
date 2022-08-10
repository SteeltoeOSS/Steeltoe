// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Messaging.Handler.Attributes.Support;

namespace Steeltoe.Stream.Config;

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
        return !typeof(IMessage).IsAssignableFrom(parameter.ParameterType) && !typeof(MessageHeaders).IsAssignableFrom(parameter.ParameterType) &&
            parameter.GetCustomAttribute<HeaderAttribute>() == null && parameter.GetCustomAttribute<HeadersAttribute>() == null;
    }

    public override object ResolveArgument(ParameterInfo parameter, IMessage message)
    {
        var ann = parameter.GetCustomAttribute<PayloadAttribute>();

        if (ann != null && !string.IsNullOrEmpty(ann.Expression))
        {
            throw new InvalidOperationException("PayloadAttribute SpEL expressions not supported by this resolver");
        }

        object payload = message.Payload;

        if (IsEmptyPayload(payload))
        {
            if (ann == null || ann.Required)
            {
                throw new MethodArgumentNotValidException(message, parameter, "Payload value must not be empty");
            }

            return null;
        }

        Type targetClass = parameter.ParameterType;
        Type payloadClass = payload.GetType();

        if (ConversionNotRequired(payloadClass, targetClass))
        {
            return payload;
        }

        if (Converter is ISmartMessageConverter smartConverter)
        {
            payload = smartConverter.FromMessage(message, targetClass, parameter);
        }
        else
        {
            payload = Converter.FromMessage(message, targetClass);
        }

        if (payload == null)
        {
            throw new MessageConversionException(message, $"Cannot convert from [{payloadClass.Name}] to [{targetClass.Name}] for {message}");
        }

        return payload;
    }

    private bool ConversionNotRequired(Type leftType, Type rightType)
    {
        return rightType == typeof(object) ? ClassUtils.IsAssignable(leftType, rightType) : ClassUtils.IsAssignable(rightType, leftType);
    }
}
