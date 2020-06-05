// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
