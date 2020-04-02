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

using Steeltoe.Messaging.Rabbit.Data;
using Steeltoe.Messaging.Support;
using System;

namespace Steeltoe.Messaging.Rabbit.Support.Converter
{
    public class MessagingMessageConverter : IMessageConverter
    {
        public MessagingMessageConverter()
        : this(new SimpleMessageConverter(), new SimpleAmqpHeaderMapper())
        {
        }

        public MessagingMessageConverter(IMessageConverter payloadConverter, IHeaderMapper<MessageProperties> headerMapper)
        {
            if (payloadConverter == null)
            {
                throw new ArgumentNullException(nameof(payloadConverter));
            }

            if (headerMapper == null)
            {
                throw new ArgumentNullException(nameof(headerMapper));
            }

            PayloadConverter = payloadConverter;
            HeaderMapper = headerMapper;
        }

        public IMessageConverter PayloadConverter { get; set; }

        public IHeaderMapper<MessageProperties> HeaderMapper { get; set; }

        public object FromMessage(Message message)
        {
            if (message == null)
            {
                return null;
            }

            var mappedHeaders = HeaderMapper.ToHeaders(message.MessageProperties);
            var convertedObject = ExtractPayload(message);
            if (convertedObject == null)
            {
                throw new MessageConversionException("Message converter returned null");
            }

            var builder = (convertedObject is IMessage) ? Messaging.Support.MessageBuilder.FromMessage((IMessage)convertedObject) : Messaging.Support.MessageBuilder.WithPayload(convertedObject);
            return builder.CopyHeadersIfAbsent(mappedHeaders).Build();
        }

        public Message ToMessage(object payload, MessageProperties messageProperties)
        {
            if (!(payload is IMessage))
            {
                throw new ArgumentException("Could not convert [" + payload + "] - only [ IMessage ] is handled by this converter");
            }

            var input = (IMessage)payload;
            var amqpMessage = PayloadConverter.ToMessage(input.Payload, messageProperties);

            HeaderMapper.FromHeaders(input.Headers, messageProperties);
            return amqpMessage;
        }

        public virtual object ExtractPayload(Message message)
        {
            return PayloadConverter.FromMessage(message);
        }
    }
}
