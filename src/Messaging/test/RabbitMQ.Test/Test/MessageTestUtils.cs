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
using Steeltoe.Messaging.Rabbit.Support;

namespace Steeltoe.Messaging.Rabbit.Test
{
    internal static class MessageTestUtils
    {
        public static IMessage<byte[]> CreateTextMessage(string body, MessageHeaders properties)
        {
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(properties);
            accessor.ContentType = MimeTypeUtils.TEXT_PLAIN_VALUE;
            return Message.Create(ToBytes(body), properties);
        }

        public static IMessage<byte[]> CreateTextMessage(string body)
        {
            return CreateTextMessage(body, new MessageHeaders());
        }

        public static string ExtractText(IMessage message)
        {
            return EncodingUtils.GetDefaultEncoding().GetString((byte[])message.Payload);
        }

        public static byte[] ToBytes(string content)
        {
            return EncodingUtils.GetDefaultEncoding().GetBytes(content);
        }
    }
}
