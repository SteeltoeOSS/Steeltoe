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
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.Rabbit.Support
{
#pragma warning disable SA1402 // File may only contain a single type
    public static class RabbitMessageBuilder
    {
        public static AbstractMessageBuilder FromMessage<P>(IMessage<P> message)
        {
            return new RabbitMessageBuilder<P>(message);
        }

        public static AbstractMessageBuilder FromMessage(IMessage message, Type payloadType = null)
        {
            var genParamType = MessageBuilder.GetGenericParamType(message, payloadType);
            var typeToCreate = typeof(RabbitMessageBuilder<>).MakeGenericType(genParamType);

            return (AbstractMessageBuilder)Activator.CreateInstance(
                  typeToCreate,
                  BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                  null,
                  new object[] { message },
                  null,
                  null);
        }

        public static AbstractMessageBuilder WithPayload<P>(P payload)
        {
            return new RabbitMessageBuilder<P>(payload, new RabbitHeaderAccessor());
        }

        public static AbstractMessageBuilder WithPayload(object payload, Type payloadType = null)
        {
            var genParamType = MessageBuilder.GetGenericParamType(payload, payloadType);
            var typeToCreate = typeof(RabbitMessageBuilder<>).MakeGenericType(genParamType);

            return (AbstractMessageBuilder)Activator.CreateInstance(
                  typeToCreate,
                  BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                  null,
                  new object[] { payload, new RabbitHeaderAccessor() },
                  null,
                  null);
        }

        public static IMessage CreateMessage(object payload, IMessageHeaders messageHeaders, Type payloadType = null)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            if (messageHeaders == null)
            {
                throw new ArgumentNullException(nameof(messageHeaders));
            }

            return Message.Create(payload, messageHeaders, payloadType);
        }
    }

    public class RabbitMessageBuilder<P> : MessageBuilder<P>
    {
        protected internal RabbitMessageBuilder()
        {
        }

        protected internal RabbitMessageBuilder(IMessage<P> message)
            : base(message)
        {
        }

        protected internal RabbitMessageBuilder(IMessage message)
            : base(message)
        {
        }

        protected internal RabbitMessageBuilder(RabbitHeaderAccessor accessor)
            : base(accessor)
        {
        }

        protected internal RabbitMessageBuilder(P payload, RabbitHeaderAccessor accessor)
            : base(payload, accessor)
        {
        }
    }
#pragma warning restore SA1402 // File may only contain a single type
}