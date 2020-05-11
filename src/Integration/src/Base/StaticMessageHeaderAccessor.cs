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
using Steeltoe.Integration.Acks;
using Steeltoe.Messaging;
using System;

namespace Steeltoe.Integration
{
    public static class StaticMessageHeaderAccessor
    {
        public static Guid? GetId(IMessage message)
        {
            message.Headers.TryGetValue(MessageHeaders.ID, out var value);
            if (value == null)
            {
                return null;
            }

            return value is Guid guid ? guid : Guid.Parse(value.ToString());
        }

        public static long? GetTimestamp(IMessage message)
        {
            message.Headers.TryGetValue(MessageHeaders.TIMESTAMP, out var value);
            if (value == null)
            {
                return null;
            }

            return value is long @int ? @int : long.Parse(value.ToString());
        }

        public static MimeType GetContentType(IMessage message)
        {
            message.Headers.TryGetValue(MessageHeaders.CONTENT_TYPE, out var value);
            if (value == null)
            {
                return null;
            }

            return value is MimeType type ? type : MimeType.ToMimeType(value.ToString());
        }

        public static long? GetExpirationDate(IMessage message)
        {
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.EXPIRATION_DATE, out var value);
            if (value == null)
            {
                return null;
            }

            return value is long @long ? @long : long.Parse(value.ToString());
        }

        public static int? GetSequenceNumber(IMessage message)
        {
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SEQUENCE_NUMBER, out var value);
            if (value == null)
            {
                return null;
            }

            return int.Parse(value.ToString());
        }

        public static int? GetSequenceSize(IMessage message)
        {
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SEQUENCE_SIZE, out var value);
            if (value == null)
            {
                return null;
            }

            return int.Parse(value.ToString());
        }

        public static int? GetPriority(IMessage message)
        {
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.PRIORITY, out var value);
            if (value == null)
            {
                return null;
            }

            return int.Parse(value.ToString());
        }

        public static IAcknowledgmentCallback GetAcknowledgmentCallback(IMessage message)
        {
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.ACKNOWLEDGMENT_CALLBACK, out var value);
            return value as IAcknowledgmentCallback;
        }
    }
}
