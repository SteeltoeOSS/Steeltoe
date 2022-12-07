// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ.Extensions;

namespace Steeltoe.Messaging.RabbitMQ.Support.Converter;

public class SimpleMessageConverter : AbstractMessageConverter
{
    public const string DefaultServiceName = nameof(SimpleMessageConverter);

    public string DefaultCharset { get; set; } = "utf-8";

    public override string ServiceName { get; set; } = DefaultServiceName;

    public SimpleMessageConverter(ILogger<SimpleMessageConverter> logger = null)
        : base(logger)
    {
    }

    public override object FromMessage(IMessage message, Type targetType, object conversionHint)
    {
        if (message is not IMessage<byte[]> bytesMessage)
        {
            throw new MessageConversionException($"Failed to convert non byte[] Message content{message.GetType()}");
        }

        object content = null;
        IMessageHeaders properties = bytesMessage.Headers;

        if (properties != null)
        {
            string contentType = properties.ContentType();

            if (contentType != null && contentType.StartsWith("text", StringComparison.Ordinal))
            {
                string encoding = properties.ContentEncoding() ?? DefaultCharset;

                try
                {
                    Encoding enc = EncodingUtils.GetEncoding(encoding);
                    content = enc.GetString(bytesMessage.Payload);
                }
                catch (Exception e)
                {
                    throw new MessageConversionException("failed to convert text-based Message content", e);
                }
            }
            else if (contentType != null && contentType == MessageHeaders.ContentTypeDotNetSerializedObject)
            {
                try
                {
                    var formatter = new BinaryFormatter();
                    using var stream = new MemoryStream(bytesMessage.Payload);

#pragma warning disable SYSLIB0011 // Type or member is obsolete
                    content = formatter.Deserialize(stream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
                }
                catch (Exception e)
                {
                    throw new MessageConversionException("failed to convert serialized Message content", e);
                }
            }
            else if (contentType != null && contentType == MessageHeaders.ContentTypeJavaSerializedObject)
            {
                throw new MessageConversionException($"Content type: {MessageHeaders.ContentTypeJavaSerializedObject} unsupported");
            }
        }

        if (content == null)
        {
            Logger?.LogDebug("FromMessage() returning message payload unchanged");
            content = bytesMessage.Payload;
        }

        return content;
    }

    protected override IMessage CreateMessage(object payload, IMessageHeaders headers, object conversionHint)
    {
        byte[] bytes = null;
        RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(headers);

        switch (payload)
        {
            case byte[] v:
                bytes = v;
                accessor.ContentType = MessageHeaders.ContentTypeBytes;
                break;
            case string sPayload:
            {
                try
                {
                    Encoding enc = EncodingUtils.GetEncoding(DefaultCharset);
                    bytes = enc.GetBytes(sPayload);
                }
                catch (Exception e)
                {
                    throw new MessageConversionException("failed to convert to Message content", e);
                }

                accessor.ContentType = MessageHeaders.ContentTypeTextPlain;
                accessor.ContentEncoding = DefaultCharset;
                break;
            }
            default:
                if (payload != null && payload.GetType().IsSerializable)
                {
                    bytes = SerializeObject(payload);
                    accessor.ContentType = MessageHeaders.ContentTypeDotNetSerializedObject;
                }

                break;
        }

        if (bytes == null)
        {
            throw new ArgumentException(
                $"{nameof(SimpleMessageConverter)} only supports string, byte[] and binary serializable payloads, received: {payload?.GetType().Name}",
                nameof(payload));
        }

        IMessage<byte[]> message = Message.Create(bytes, headers);
        accessor.ContentLength = bytes.Length;
        return message;
    }

    private static byte[] SerializeObject(object payload)
    {
        try
        {
            var formatter = new BinaryFormatter();
            using var stream = new MemoryStream(512);

#pragma warning disable SYSLIB0011 // Type or member is obsolete
            formatter.Serialize(stream, payload);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            return stream.ToArray();
        }
        catch (Exception e)
        {
            throw new MessageConversionException("failed to convert serialized Message content", e);
        }
    }
}
