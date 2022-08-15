// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging.Support;

namespace Steeltoe.Messaging.Converter;

public class SimpleMessageConverter : IMessageConverter
{
    public const string DefaultServiceName = nameof(SimpleMessageConverter);

    public string ServiceName { get; set; } = DefaultServiceName;

    public virtual object FromMessage(IMessage message, Type targetType)
    {
        object payload = message.Payload;
        return ClassUtils.IsAssignableValue(targetType, payload) ? payload : null;
    }

    public virtual T FromMessage<T>(IMessage message)
    {
        return (T)FromMessage(message, typeof(T));
    }

    public virtual IMessage ToMessage(object payload, IMessageHeaders headers)
    {
        if (headers != null)
        {
            MessageHeaderAccessor accessor = MessageHeaderAccessor.GetAccessor(headers, typeof(MessageHeaderAccessor));

            if (accessor != null && accessor.IsMutable)
            {
                return MessageBuilder.CreateMessage(payload, accessor.MessageHeaders);
            }
        }

        return MessageBuilder.WithPayload(payload).CopyHeaders(headers).Build();
    }
}
