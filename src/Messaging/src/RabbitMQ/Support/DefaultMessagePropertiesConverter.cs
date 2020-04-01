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
using RabbitMQ.Client;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Messaging.Rabbit.Support
{
    public class DefaultMessagePropertiesConverter : IMessagePropertiesConverter
    {
        private const int DEFAULT_LONG_STRING_LIMIT = 1024;
        private readonly ILogger _logger;
        private readonly int _longStringLimit;
        private readonly bool _convertLongLongStrings;

        public DefaultMessagePropertiesConverter(ILogger logger = null)
            : this(DEFAULT_LONG_STRING_LIMIT, false)
        {
            _logger = logger;
        }

        public DefaultMessagePropertiesConverter(int longStringLimit)
            : this(longStringLimit, false)
        {
        }

        public DefaultMessagePropertiesConverter(int longStringLimit, bool convertLongLongStrings)
        {
            _longStringLimit = longStringLimit;
            _convertLongLongStrings = convertLongLongStrings;
        }

        public IBasicProperties FromMessageProperties(MessageProperties source, IBasicProperties target, Encoding charset)
        {
            target.Headers = ConvertHeadersIfNecessary(source.Headers);
            if (source.Timestamp.HasValue)
            {
                var offset = new DateTimeOffset(source.Timestamp.Value);
                target.Timestamp = new AmqpTimestamp(offset.ToUnixTimeMilliseconds());
            }

            target.MessageId = source.MessageId;
            target.UserId = source.UserId;
            target.AppId = source.AppId;
            target.ClusterId = source.ClusterId;
            target.Type = source.Type;
            if (source.DeliveryMode.HasValue)
            {
                target.DeliveryMode = (byte)source.DeliveryMode.Value;
            }

            target.Expiration = source.Expiration;
            if (source.Priority.HasValue)
            {
                target.Priority = (byte)source.Priority.Value;
            }

            target.ContentType = source.ContentType;
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

            return target;
        }

        public MessageProperties ToMessageProperties(IBasicProperties source, Envelope envelope, Encoding charset)
        {
            var target = new MessageProperties();
            var headers = source.Headers;
            if (headers.Count > 0)
            {
                foreach (var entry in headers)
                {
                    var key = entry.Key;
                    if (MessageProperties.X_DELAY.Equals(key))
                    {
                        var value = entry.Value;
                        if (value is int)
                        {
                            target.ReceivedDelay = (int)value;
                        }
                    }
                    else
                    {
                        target.SetHeader(key, entry.Value);
                    }
                }
            }

            target.Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(source.Timestamp.UnixTime).DateTime;
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

            return target;
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
                writableHeaders[entry.Key] = ConvertHeaderValueIfNecessary(entry.Value);
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
                || (value is short) || (value is AmqpTimestamp)
                || (value is IList) || (value is IDictionary) || (value is BinaryTableValue)
                || (value is object[]) || (value is Type);

            if (!valid)
            {
                value = value.ToString();
            }
            else if (value is object[] array)
            {
                var writableList = new List<object>();
                for (var i = 0; i < array.Length; i++)
                {
                    writableList.Add(ConvertHeaderValueIfNecessary(array[i]));
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
                value = ((Type)value).FullName;
            }

            return value;
        }
    }
}
