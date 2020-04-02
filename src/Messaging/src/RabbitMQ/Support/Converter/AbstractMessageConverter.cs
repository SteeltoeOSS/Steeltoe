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
using System;

namespace Steeltoe.Messaging.Rabbit.Support.Converter
{
    public abstract class AbstractMessageConverter : IMessageConverter
    {
        public bool CreateMessageIds { get; set; }

        public abstract object FromMessage(Message message);

        public Message ToMessage(object payload, MessageProperties messageProperties)
        {
            return ToMessage(payload, messageProperties, null);
        }

        public Message ToMessage(object payload, MessageProperties messagePropertiesArg, Type genericType)
        {
            var messageProperties = messagePropertiesArg;
            if (messageProperties == null)
            {
                messageProperties = new MessageProperties();
            }

            var message = CreateMessage(payload, messageProperties, genericType);
            messageProperties = message.MessageProperties;
            if (CreateMessageIds && messageProperties.MessageId == null)
            {
                messageProperties.MessageId = Guid.NewGuid().ToString();
            }

            return message;
        }

        protected virtual Message CreateMessage(object payload, MessageProperties messageProperties, Type genericType)
        {
            return CreateMessage(payload, messageProperties);
        }

        protected abstract Message CreateMessage(object payload, MessageProperties messageProperties);
    }
}
