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

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Rabbit.Extensions;
using Steeltoe.Messaging.Support;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Steeltoe.Messaging.Rabbit.Support.Converter
{
    public class SimpleMessageConverter : AbstractMessageConverter
    {
        public const string DEFAULT_SERVICE_NAME = nameof(SimpleMessageConverter);

        public string DefaultCharset { get; set; } = "utf-8";

        public override string ServiceName { get; set; } = DEFAULT_SERVICE_NAME;

        public SimpleMessageConverter(ILogger<SimpleMessageConverter> logger = null)
            : base(logger)
        {
        }

        public override object FromMessage(IMessage from, Type targetType, object convertionsHint)
        {
            IMessage<byte[]> message = from as IMessage<byte[]>;
            if (message == null)
            {
                throw new MessageConversionException("Failed to convert non byte[] Message content" + from.GetType());
            }

            object content = null;
            var properties = message.Headers;
            if (properties != null)
            {
                var contentType = properties.ContentType();
                if (contentType != null && contentType.StartsWith("text"))
                {
                    var encoding = properties.ContentEncoding();
                    if (encoding == null)
                    {
                        encoding = DefaultCharset;
                    }

                    try
                    {
                        var enc = EncodingUtils.GetEncoding(encoding);
                        content = enc.GetString(message.Payload);
                    }
                    catch (Exception e)
                    {
                        throw new MessageConversionException(
                                "failed to convert text-based Message content", e);
                    }
                }
                else if (contentType != null &&
                    contentType.Equals(MessageHeaders.CONTENT_TYPE_DOTNET_SERIALIZED_OBJECT))
                {
                    try
                    {
                        var formatter = new BinaryFormatter();
                        var stream = new MemoryStream(message.Payload);
                        content = formatter.Deserialize(stream);
                    }
                    catch (Exception e)
                    {
                        throw new MessageConversionException("failed to convert serialized Message content", e);
                    }
                }
                else if (contentType != null &&
                        contentType.Equals(MessageHeaders.CONTENT_TYPE_JAVA_SERIALIZED_OBJECT))
                {
                    throw new MessageConversionException("Content type: " + MessageHeaders.CONTENT_TYPE_JAVA_SERIALIZED_OBJECT + " unsupported");
                }
            }

            if (content == null)
            {
                _logger?.LogDebug("FromMessage() returning message payload unchanged");
                content = message.Payload;
            }

            return content;
        }

        protected override IMessage CreateMessage(object payload, IMessageHeaders messageProperties, object conversionHint)
        {
            byte[] bytes = null;
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(messageProperties);
            if (payload is byte[])
            {
                bytes = (byte[])payload;
                accessor.ContentType = MessageHeaders.CONTENT_TYPE_BYTES;
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

                accessor.ContentType = MessageHeaders.CONTENT_TYPE_TEXT_PLAIN;
                accessor.ContentEncoding = DefaultCharset;
            }
            else if (payload.GetType().IsSerializable)
            {
                try
                {
                    var formatter = new BinaryFormatter();
                    var stream = new MemoryStream(512);
                    formatter.Serialize(stream, payload);
                    bytes = stream.ToArray();
                    accessor.ContentType = MessageHeaders.CONTENT_TYPE_DOTNET_SERIALIZED_OBJECT;
                }
                catch (Exception e)
                {
                    throw new MessageConversionException("failed to convert serialized Message content", e);
                }
            }

            if (bytes == null)
            {
                throw new ArgumentException("SimpleMessageConverter only supports string, byte[] and serializable payloads, received: " + payload?.GetType().Name);
            }

            var message = Message.Create(bytes, messageProperties);
            accessor.ContentLength = bytes.Length;
            return message;
        }
    }
}
