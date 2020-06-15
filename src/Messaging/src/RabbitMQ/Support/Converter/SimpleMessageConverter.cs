// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging.Rabbit.Data;
using System;

namespace Steeltoe.Messaging.Rabbit.Support.Converter
{
    public class SimpleMessageConverter : AbstractMessageConverter
    {
        public string DefaultCharset { get; set; } = "utf-8";

        public override object FromMessage(Message message)
        {
            object content = null;
            var properties = message.MessageProperties;
            if (properties != null)
            {
                var contentType = properties.ContentType;
                if (contentType != null && contentType.StartsWith("text"))
                {
                    var encoding = properties.ContentEncoding;
                    if (encoding == null)
                    {
                        encoding = DefaultCharset;
                    }

                    try
                    {
                        var enc = EncodingUtils.GetEncoding(encoding);
                        content = enc.GetString(message.Body);
                    }
                    catch (Exception e)
                    {
                        throw new MessageConversionException(
                                "failed to convert text-based Message content", e);
                    }
                }
                else if (contentType != null &&
                        contentType.Equals(MessageProperties.CONTENT_TYPE_SERIALIZED_OBJECT))
                {
                    throw new MessageConversionException("Content type: " + MessageProperties.CONTENT_TYPE_SERIALIZED_OBJECT + " unsupported");
                }
            }

            if (content == null)
            {
                content = message.Body;
            }

            return content;
        }

        protected override Message CreateMessage(object payload, MessageProperties messageProperties)
        {
            byte[] bytes = null;
            if (payload is byte[])
            {
                bytes = (byte[])payload;
                messageProperties.ContentType = MessageProperties.CONTENT_TYPE_BYTES;
            }
            else if (payload is string)
            {
                try
                {
                    var enc = EncodingUtils.GetEncoding(DefaultCharset);
                    bytes = enc.GetBytes((string)payload);
                }
                catch (Exception e)
                {
                    throw new MessageConversionException("failed to convert to Message content", e);
                }

                messageProperties.ContentType = MessageProperties.CONTENT_TYPE_TEXT_PLAIN;
                messageProperties.ContentEncoding = DefaultCharset;
            }

            if (bytes != null)
            {
                messageProperties.ContentLength = bytes.Length;
                return new Message(bytes, messageProperties);
            }

            throw new ArgumentException("SimpleMessageConverter only supports String, and byte[] payloads, received: " + payload?.GetType().Name);
        }
    }
}
