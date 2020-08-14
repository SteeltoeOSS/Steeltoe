// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using RC=RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Support
{
    public class DefaultMessageHeadersConverter : IMessageHeadersConverter
    {
        private const int DEFAULT_LONG_STRING_LIMIT = 1024;
        private readonly ILogger _logger;
        private readonly int _longStringLimit;
        private readonly bool _convertLongLongStrings;

        public DefaultMessageHeadersConverter(ILogger logger = null)
            : this(DEFAULT_LONG_STRING_LIMIT, false)
        {
            _logger = logger;
        }

        public DefaultMessageHeadersConverter(int longStringLimit)
            : this(longStringLimit, false)
        {
        }

        public DefaultMessageHeadersConverter(int longStringLimit, bool convertLongLongStrings)
        {
            _longStringLimit = longStringLimit;
            _convertLongLongStrings = convertLongLongStrings;
        }

        public virtual void FromMessageHeaders(IMessageHeaders headers, RC.IBasicProperties target, Encoding charset)
        {
            var source = headers;
            target.Headers = ConvertHeadersIfNecessary(headers);
            if (source.Timestamp.HasValue)
            {
                target.Timestamp = new RC.AmqpTimestamp(source.Timestamp.Value);
            }

            if (source.Id != null)
            {
                target.MessageId = source.Id;
            }

            if (source.UserId() != null)
            {
                target.UserId = source.UserId();
            }

            if (source.AppId() != null)
            {
                target.AppId = source.AppId();
            }

            if (source.ClusterId() != null)
            {
                target.ClusterId = source.ClusterId();
            }

            if (source.Type() != null)
            {
                target.Type = source.Type();
            }

            if (source.DeliveryMode().HasValue)
            {
                target.DeliveryMode = (byte)source.DeliveryMode().Value;
            }
            else
            {
                target.DeliveryMode = (byte)RabbitHeaderAccessor.DEFAULT_DELIVERY_MODE;
            }

            if (source.Expiration() != null)
            {
                target.Expiration = source.Expiration();
            }

            if (source.Priority().HasValue)
            {
                target.Priority = (byte)source.Priority().Value;
            }

            if (source.ContentType() != null)
            {
                target.ContentType = source.ContentType();
            }

            if (string.IsNullOrEmpty(target.ContentType))
            {
                target.ContentType = RabbitHeaderAccessor.DEFAULT_CONTENT_TYPE;
            }

            if (source.ContentEncoding() != null)
            {
                target.ContentEncoding = source.ContentEncoding();
            }

            var correlationId = source.CorrelationId();
            if (!string.IsNullOrEmpty(correlationId))
            {
                target.CorrelationId = correlationId;
            }

            var replyTo = source.ReplyTo();
            if (replyTo != null)
            {
                target.ReplyTo = replyTo;
            }
        }

        public virtual IMessageHeaders ToMessageHeaders(RC.IBasicProperties source, Envelope envelope, Encoding charset)
        {
            var target = new RabbitHeaderAccessor();
            var headers = source.Headers;
            if (headers?.Count > 0)
            {
                foreach (var entry in headers)
                {
                    var key = entry.Key;
                    if (RabbitMessageHeaders.X_DELAY.Equals(key))
                    {
                        var value = entry.Value;
                        if (value is int)
                        {
                            target.ReceivedDelay = (int)value;
                        }
                    }
                    else
                    {
                        target.SetHeader(key, ConvertLongStringIfNecessary(entry.Value, charset));
                    }
                }
            }

            target.Timestamp = source.Timestamp.UnixTime;
            target.MessageId = source.MessageId;
            target.ReceivedUserId = source.UserId;
            target.AppId = source.AppId;
            target.ClusterId = source.ClusterId;
            target.Type = source.Type;
            var deliveryMode = (int)source.DeliveryMode;
            target.ReceivedDeliveryMode = (MessageDeliveryMode)Enum.ToObject(typeof(MessageDeliveryMode), deliveryMode);
            target.DeliveryMode = null;
            target.Expiration = source.Expiration;
            target.Priority = source.Priority;

            target.ContentType = source.ContentType;
            if (string.IsNullOrEmpty(target.ContentType))
            {
                target.ContentType = RabbitHeaderAccessor.DEFAULT_CONTENT_TYPE;
            }

            target.ContentEncoding = source.ContentEncoding;
            var correlationId = source.CorrelationId;
            if (!string.IsNullOrEmpty(correlationId))
            {
                target.CorrelationId = correlationId;
            }

            var replyTo = source.ReplyTo;
            if (replyTo != null)
            {
                target.ReplyTo = replyTo;
            }

            if (envelope != null)
            {
                target.ReceivedExchange = envelope.Exchange;
                target.ReceivedRoutingKey = envelope.RoutingKey;
                target.Redelivered = envelope.Redeliver;
                target.DeliveryTag = envelope.DeliveryTag;
            }

            target.LeaveMutable = true;
            return target.MessageHeaders;
        }

        private IDictionary<string, object> ConvertHeadersIfNecessary(IDictionary<string, object> source)
        {
            if (source.Count == 0)
            {
                return source;
            }

            var writableHeaders = new Dictionary<string, object>();
            foreach (var entry in source)
            {
                if (!entry.Key.StartsWith(MessageHeaders.INTERNAL) &&
                    !entry.Key.StartsWith(RabbitMessageHeaders.RABBIT_PROPERTY) &&
                    entry.Key != MessageHeaders.ID &&
                    entry.Key != MessageHeaders.TIMESTAMP)
                {
                    writableHeaders[entry.Key] = ConvertHeaderValueIfNecessary(entry.Value);
                }
            }

            return writableHeaders;
        }

        private object ConvertHeaderValueIfNecessary(object valueArg)
        {
            var value = valueArg;
            if (value == null)
            {
                return value;
            }

            var valid = (value is string) || (value is byte[])
                || (value is bool) || (value is byte) || (value is sbyte)
                || (value is uint) || (value is int) || (value is long)
                || (value is float) || (value is double) || (value is decimal)
                || (value is short) || (value is RC.AmqpTimestamp)
                || (value is IList) || (value is IDictionary) || (value is RC.BinaryTableValue)
                || (value is object[]) || (value is Type);

            if (!valid)
            {
                value = value.ToString();
            }
            else if (value is object[] array)
            {
                var writableList = new object[array.Length];
                for (var i = 0; i < array.Length; i++)
                {
                    writableList[i] = ConvertHeaderValueIfNecessary(array[i]);
                }

                value = writableList;
            }
            else if (value is IList)
            {
                var writableList = new List<object>();
                foreach (var listValue in (IList)value)
                {
                    writableList.Add(ConvertHeaderValueIfNecessary(listValue));
                }

                value = writableList;
            }
            else if (value is IDictionary originalMap)
            {
                var writableMap = new Dictionary<object, object>();
                foreach (DictionaryEntry entry in originalMap)
                {
                    writableMap[entry.Key] = ConvertHeaderValueIfNecessary(entry.Value);
                }

                value = writableMap;
            }
            else if (value is Type)
            {
                value = ((Type)value).ToString();
            }
            else if (value is byte[])
            {
                value =  new RC.BinaryTableValue((byte[])value);
            }

            return value;
        }

        private object ConvertLongStringIfNecessary(object valueArg, Encoding charset)
        {
            if (valueArg is byte[])
            {
                try
                {
                    return charset.GetString((byte[])valueArg);
                }
                catch (Exception)
                {
                    // Log
                }
            }
            else if (valueArg is List<object>)
            {
                var convertedList = new List<object>();
                foreach (var listValue in (List<object>)valueArg)
                {
                    convertedList.Add(ConvertLongStringIfNecessary(listValue, charset));
                }

                return convertedList;
            }

            return valueArg;
        }
    }
}
