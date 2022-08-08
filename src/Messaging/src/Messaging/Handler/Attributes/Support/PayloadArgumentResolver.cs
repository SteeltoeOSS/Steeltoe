// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Invocation;

namespace Steeltoe.Messaging.Handler.Attributes.Support;

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
        ArgumentGuard.NotNull(messageConverter);

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

        if (targetClass.IsAssignableFrom(payloadClass))
        {
            return payload;
        }

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
            throw new MessageConversionException(message, $"Cannot convert from [{payloadClass.Name}] to [{targetClass.Name}] for {message}");
        }

        return payload;
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
}
