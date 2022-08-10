// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Invocation;

namespace Steeltoe.Messaging.Handler.Attributes.Support;

public class PayloadMethodArgumentResolver : IHandlerMethodArgumentResolver
{
    protected readonly IMessageConverter Converter;
    protected readonly bool UseDefaultResolution;

    public PayloadMethodArgumentResolver(IMessageConverter messageConverter)
        : this(messageConverter, true)
    {
    }

    public PayloadMethodArgumentResolver(IMessageConverter messageConverter, bool useDefaultResolution)
    {
        Converter = messageConverter ?? throw new ArgumentNullException(nameof(messageConverter));
        UseDefaultResolution = useDefaultResolution;
    }

    public virtual bool SupportsParameter(ParameterInfo parameter)
    {
        return parameter.GetCustomAttribute<PayloadAttribute>() != null || UseDefaultResolution;
    }

    public virtual object ResolveArgument(ParameterInfo parameter, IMessage message)
    {
        var ann = parameter.GetCustomAttribute<PayloadAttribute>();

        if (ann != null && !string.IsNullOrEmpty(ann.Expression))
        {
            throw new InvalidOperationException("Payload expressions not supported by this resolver");
        }

        object payload = message.Payload;

        if (IsEmptyPayload(payload))
        {
            if (ann == null || ann.Required)
            {
                string paramName = GetParameterName(parameter);
                throw new MethodArgumentNotValidException(message, parameter, $"Payload value must not be empty when binding to: {paramName}");
            }

            return null;
        }

        Type targetClass = ResolveTargetClass(parameter, message);
        Type payloadClass = payload.GetType();

        if (targetClass.IsAssignableFrom(payloadClass))
        {
            Validate(message, parameter, payload);
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

        Validate(message, parameter, payload);

        return payload;
    }

    protected virtual Type ResolveTargetClass(ParameterInfo parameter, IMessage message)
    {
        return parameter.ParameterType;
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

    protected virtual void Validate(IMessage message, ParameterInfo parameter, object target)
    {
    }

    private string GetParameterName(ParameterInfo param)
    {
        return param.Name ?? $"Arg {param.Position}";
    }
}
