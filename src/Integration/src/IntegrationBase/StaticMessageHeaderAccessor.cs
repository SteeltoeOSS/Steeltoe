// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

            return value is Guid guidValue ? guidValue : Guid.Parse(value.ToString());
        }

        public static long? GetTimestamp(IMessage message)
        {
            message.Headers.TryGetValue(MessageHeaders.TIMESTAMP, out var value);
            if (value == null)
            {
                return null;
            }

            return value is long longValue ? longValue : long.Parse(value.ToString());
        }

        public static MimeType GetContentType(IMessage message)
        {
            message.Headers.TryGetValue(MessageHeaders.CONTENT_TYPE, out var value);
            if (value == null)
            {
                return null;
            }

            return value as MimeType ?? MimeType.ToMimeType(value.ToString());
        }

        public static long? GetExpirationDate(IMessage message)
        {
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.EXPIRATION_DATE, out var value);
            if (value == null)
            {
                return null;
            }

            return value is long longValue ? longValue : long.Parse(value.ToString());
        }

        public static int? GetSequenceNumber(IMessage message)
        {
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SEQUENCE_NUMBER, out var value);
            return value != null ? int.Parse(value.ToString()) : null;
        }

        public static int? GetSequenceSize(IMessage message)
        {
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.SEQUENCE_SIZE, out var value);
            return value != null ? int.Parse(value.ToString()) : null;
        }

        public static int? GetPriority(IMessage message)
        {
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.PRIORITY, out var value);
            return value != null ? int.Parse(value.ToString()) : null;
        }

        public static IAcknowledgmentCallback GetAcknowledgmentCallback(IMessage message)
        {
            message.Headers.TryGetValue(IntegrationMessageHeaderAccessor.ACKNOWLEDGMENT_CALLBACK, out var value);
            return value as IAcknowledgmentCallback;
        }
    }
}
