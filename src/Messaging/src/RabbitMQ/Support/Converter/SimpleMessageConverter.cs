// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
