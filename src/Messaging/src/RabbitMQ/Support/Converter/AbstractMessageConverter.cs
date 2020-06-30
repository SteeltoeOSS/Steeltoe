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
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Rabbit.Extensions;
using System;
using System.Net.Sockets;

namespace Steeltoe.Messaging.Rabbit.Support.Converter
{
    public abstract class AbstractMessageConverter : ISmartMessageConverter
    {
        protected readonly ILogger _logger;

        protected AbstractMessageConverter(ILogger logger = null)
        {
            _logger = logger;
        }

        public bool CreateMessageIds { get; set; }

        public abstract string ServiceName { get; set; }

        public abstract object FromMessage(IMessage message, Type targetClass, object conversionHint);

        public T FromMessage<T>(IMessage message, object conversionHint)
        {
            return (T)FromMessage(message, typeof(T), conversionHint);
        }

        public object FromMessage(IMessage message, Type targetClass)
        {
            return FromMessage(message, targetClass, null);
        }

        public T FromMessage<T>(IMessage message)
        {
            return (T)FromMessage(message, typeof(T), null);
        }

        public IMessage ToMessage(object payload, IMessageHeaders messageProperties)
        {
            return ToMessage(payload, messageProperties, null);
        }

        public IMessage ToMessage(object payload, IMessageHeaders headers, object conversionHint)
        {
            var messageProperties = headers;
            if (messageProperties == null)
            {
                messageProperties = new MessageHeaders();
            }

            var message = CreateMessage(payload, messageProperties, conversionHint);

            if (CreateMessageIds && message.Headers.MessageId() == null)
            {
                var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
                accessor.MessageId = Guid.NewGuid().ToString();
            }

            return message;
        }

        protected abstract IMessage CreateMessage(object payload, IMessageHeaders messageProperties, object conversionHint);

        protected virtual IMessage CreateMessage(object payload, IMessageHeaders messageProperties)
        {
            return CreateMessage(payload, messageProperties, null);
        }
    }
}
