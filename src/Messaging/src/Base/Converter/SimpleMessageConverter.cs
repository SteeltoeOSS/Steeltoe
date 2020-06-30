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
using Steeltoe.Messaging.Support;
using System;

namespace Steeltoe.Messaging.Converter
{
    public class SimpleMessageConverter : IMessageConverter
    {
        public const string DEFAULT_SERVICE_NAME = nameof(SimpleMessageConverter);

        public string ServiceName { get; set; } = DEFAULT_SERVICE_NAME;

        public virtual object FromMessage(IMessage message, Type targetClass)
        {
            var payload = message.Payload;
            return ClassUtils.IsAssignableValue(targetClass, payload) ? payload : null;
        }

        public virtual T FromMessage<T>(IMessage message)
        {
            return (T)FromMessage(message, typeof(T));
        }

        public virtual IMessage ToMessage(object payload, IMessageHeaders headers)
        {
            if (headers != null)
            {
                var accessor = MessageHeaderAccessor.GetAccessor(headers, typeof(MessageHeaderAccessor));
                if (accessor != null && accessor.IsMutable)
                {
                    return MessageBuilder.CreateMessage(payload, accessor.MessageHeaders);
                }
            }

            return MessageBuilder.WithPayload(payload).CopyHeaders(headers).Build();
        }
    }
}
