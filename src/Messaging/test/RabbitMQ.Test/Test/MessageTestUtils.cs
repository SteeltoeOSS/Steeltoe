// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Support;

namespace Steeltoe.Messaging.RabbitMQ.Test;

internal static class MessageTestUtils
{
    public static IMessage<byte[]> CreateTextMessage(string body, MessageHeaders properties)
    {
        var accessor = RabbitHeaderAccessor.GetMutableAccessor(properties);
        accessor.ContentType = MimeTypeUtils.TextPlainValue;
        return Message.Create(ToBytes(body), properties);
    }

    public static IMessage<byte[]> CreateTextMessage(string body)
    {
        return CreateTextMessage(body, new MessageHeaders());
    }

    public static string ExtractText(IMessage message)
    {
        return EncodingUtils.GetDefaultEncoding().GetString((byte[])message.Payload);
    }

    public static byte[] ToBytes(string content)
    {
        return EncodingUtils.GetDefaultEncoding().GetBytes(content);
    }
}
