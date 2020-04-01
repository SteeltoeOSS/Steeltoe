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

using System;

namespace Steeltoe.Messaging.Rabbit.Data
{
    public class MessageBuilder : AbstractMessageBuilder<Message>
    {
        private byte[] _body;

        public static MessageBuilder WithBody(byte[] body)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            return new MessageBuilder(body);
        }

        public static MessageBuilder WithBody(byte[] body, int from, int to)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            var len = to - from;
            var copy = new byte[len];
            Array.Copy(body, from, copy, 0, len);
            return new MessageBuilder(copy);
        }

        public static MessageBuilder WithClonedBody(byte[] body)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            return new MessageBuilder((byte[])body.Clone());
        }

        public static MessageBuilder FromMessage(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return new MessageBuilder(message);
        }

        public static MessageBuilder FromClonedMessage(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var body = message.Body;
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            return new MessageBuilder((byte[])body.Clone(), message.MessageProperties);
        }

        private MessageBuilder(byte[] body)
        {
            _body = body;
        }

        private MessageBuilder(Message message)
            : this(message.Body, message.MessageProperties)
        {
        }

        private MessageBuilder(byte[] body, MessageProperties properties)
        {
            _body = body;
            CopyProperties(properties);
        }

        public MessageBuilder AndProperties(MessageProperties properties)
        {
            Properties = properties;
            return this;
        }

        public override Message Build()
        {
            return new Message(_body, Properties);
        }
    }
}
