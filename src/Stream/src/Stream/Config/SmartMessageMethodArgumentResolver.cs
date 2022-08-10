// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Messaging.Support;

namespace Steeltoe.Stream.Config;

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
        Type targetPayloadType = GetPayloadType(parameter, message);

        Type payloadClass = message.Payload.GetType();

        if (message is ErrorMessage || ConversionNotRequired(payloadClass, targetPayloadType))
        {
            return message;
        }

        object payload = message.Payload;

        if (IsEmptyPayload(payload))
        {
            throw new MessageConversionException(message,
                $"Cannot convert from actual payload type '{payload.GetType()}' to expected payload type '{targetPayloadType}' when payload is empty");
        }

        payload = ConvertPayload(message, parameter, targetPayloadType);
        return MessageBuilder.CreateMessage(payload, message.Headers, targetPayloadType);
    }

    protected override bool IsEmptyPayload(object payload)
    {
        return payload switch
        {
            null => true,
            byte[] v => v.Length == 0,
            string sPayload => string.IsNullOrEmpty(sPayload),
            _ => false
        };
    }

    private bool ConversionNotRequired(Type leftType, Type rightType)
    {
        return rightType == typeof(object) ? ClassUtils.IsAssignable(leftType, rightType) : ClassUtils.IsAssignable(rightType, leftType);
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
            throw new MessageConversionException(message,
                $"No converter found from actual payload type '{message.Payload.GetType()}' to expected payload type '{targetPayloadType}'");
        }

        return result;
    }
}
