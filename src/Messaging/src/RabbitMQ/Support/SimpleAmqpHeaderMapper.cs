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
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Data;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Support
{
    public class SimpleAmqpHeaderMapper : AbstractHeaderMapper<MessageProperties>
    {
        public SimpleAmqpHeaderMapper(ILogger logger = null)
            : base(logger)
        {
        }

        public override void FromHeaders(IMessageHeaders headers, MessageProperties amqpMessageProperties)
        {
            var value = GetHeaderIfAvailable<string>(headers, AmqpHeaders.APP_ID);
            if (!string.IsNullOrEmpty(value))
            {
                amqpMessageProperties.AppId = value;
            }

            value = GetHeaderIfAvailable<string>(headers, AmqpHeaders.CLUSTER_ID);
            if (!string.IsNullOrEmpty(value))
            {
                amqpMessageProperties.ClusterId = value;
            }

            value = GetHeaderIfAvailable<string>(headers, AmqpHeaders.CONTENT_ENCODING);
            if (!string.IsNullOrEmpty(value))
            {
                amqpMessageProperties.ContentEncoding = value;
            }

            amqpMessageProperties.ContentLength = GetHeaderIfAvailable<long?>(headers, AmqpHeaders.CONTENT_LENGTH);

            value = ExtractContentTypeAsString(headers);
            if (!string.IsNullOrEmpty(value))
            {
                amqpMessageProperties.ContentType = value;
            }

            var correlationId = headers.Get<object>(AmqpHeaders.CORRELATION_ID);
            if (correlationId is string)
            {
                amqpMessageProperties.CorrelationId = (string)correlationId;
            }

            amqpMessageProperties.ContentLength = GetHeaderIfAvailable<int?>(headers, AmqpHeaders.DELAY);
            amqpMessageProperties.DeliveryMode = GetHeaderIfAvailable<MessageDeliveryMode?>(headers, AmqpHeaders.DELIVERY_MODE);
            amqpMessageProperties.DeliveryTag = GetHeaderIfAvailable<ulong?>(headers, AmqpHeaders.DELIVERY_TAG);

            value = GetHeaderIfAvailable<string>(headers, AmqpHeaders.EXPIRATION);
            if (!string.IsNullOrEmpty(value))
            {
                amqpMessageProperties.Expiration = value;
            }

            amqpMessageProperties.MessageCount = GetHeaderIfAvailable<uint?>(headers, AmqpHeaders.MESSAGE_COUNT);

            value = GetHeaderIfAvailable<string>(headers, AmqpHeaders.MESSAGE_ID);
            if (!string.IsNullOrEmpty(value))
            {
                amqpMessageProperties.MessageId = value;
            }

            amqpMessageProperties.Priority = GetHeaderIfAvailable<int?>(headers, AmqpMessageHeaderAccessor.PRIORITY);

            value = GetHeaderIfAvailable<string>(headers, AmqpHeaders.RECEIVED_EXCHANGE);
            if (!string.IsNullOrEmpty(value))
            {
                amqpMessageProperties.ReceivedExchange = value;
            }

            value = GetHeaderIfAvailable<string>(headers, AmqpHeaders.RECEIVED_ROUTING_KEY);
            if (!string.IsNullOrEmpty(value))
            {
                amqpMessageProperties.ReceivedRoutingKey = value;
            }

            amqpMessageProperties.Redelivered = GetHeaderIfAvailable<bool?>(headers, AmqpHeaders.REDELIVERED);

            value = GetHeaderIfAvailable<string>(headers, AmqpHeaders.REPLY_TO);
            if (!string.IsNullOrEmpty(value))
            {
                amqpMessageProperties.ReplyTo = value;
            }

            amqpMessageProperties.Timestamp = GetHeaderIfAvailable<DateTime?>(headers, AmqpHeaders.TIMESTAMP);

            value = GetHeaderIfAvailable<string>(headers, AmqpHeaders.TYPE);
            if (!string.IsNullOrEmpty(value))
            {
                amqpMessageProperties.Type = value;
            }

            value = GetHeaderIfAvailable<string>(headers, AmqpHeaders.USER_ID);
            if (!string.IsNullOrEmpty(value))
            {
                amqpMessageProperties.UserId = value;
            }

            var replyCorrelation = GetHeaderIfAvailable<string>(headers, AmqpHeaders.SPRING_REPLY_CORRELATION);
            if (!string.IsNullOrEmpty(replyCorrelation))
            {
                amqpMessageProperties.SetHeader("spring_reply_correlation", replyCorrelation);
            }

            var replyToStack = GetHeaderIfAvailable<string>(headers, AmqpHeaders.SPRING_REPLY_TO_STACK);
            if (!string.IsNullOrEmpty(replyToStack))
            {
                amqpMessageProperties.SetHeader("spring_reply_to", replyToStack);
            }

            foreach (var entry in headers)
            {
                var headerName = entry.Key;
                if (!string.IsNullOrEmpty(headerName) && !headerName.StartsWith(AmqpHeaders.PREFIX))
                {
                    var val = entry.Value;
                    if (val != null)
                    {
                        var propertyName = FromHeaderName(headerName);
                        if (!amqpMessageProperties.Headers.ContainsKey(headerName))
                        {
                            amqpMessageProperties.SetHeader(propertyName, val);
                        }
                    }
                }
            }
        }

        public override IMessageHeaders ToHeaders(MessageProperties amqpMessageProperties)
        {
            var headers = new Dictionary<string, object>();
            try
            {
                if (!string.IsNullOrEmpty(amqpMessageProperties.AppId))
                {
                    headers.Add(AmqpHeaders.APP_ID, amqpMessageProperties.AppId);
                }

                if (!string.IsNullOrEmpty(amqpMessageProperties.ClusterId))
                {
                    headers.Add(AmqpHeaders.CLUSTER_ID, amqpMessageProperties.ClusterId);
                }

                if (!string.IsNullOrEmpty(amqpMessageProperties.ContentEncoding))
                {
                    headers.Add(AmqpHeaders.CONTENT_ENCODING, amqpMessageProperties.ContentEncoding);
                }

                if (amqpMessageProperties.ContentLength.HasValue)
                {
                    headers.Add(AmqpHeaders.CONTENT_LENGTH, amqpMessageProperties.ContentLength.Value);
                }

                if (!string.IsNullOrEmpty(amqpMessageProperties.ContentType))
                {
                    headers.Add(AmqpHeaders.CONTENT_TYPE, amqpMessageProperties.ContentType);
                }

                if (!string.IsNullOrEmpty(amqpMessageProperties.CorrelationId))
                {
                    headers.Add(AmqpHeaders.CORRELATION_ID, amqpMessageProperties.CorrelationId);
                }

                if (amqpMessageProperties.ReceivedDeliveryMode.HasValue)
                {
                    headers.Add(AmqpHeaders.RECEIVED_DELIVERY_MODE, amqpMessageProperties.ReceivedDeliveryMode.Value);
                }

                if (amqpMessageProperties.DeliveryTag.HasValue)
                {
                    headers.Add(AmqpHeaders.DELIVERY_TAG, amqpMessageProperties.DeliveryTag.Value);
                }

                if (!string.IsNullOrEmpty(amqpMessageProperties.Expiration))
                {
                    headers.Add(AmqpHeaders.EXPIRATION, amqpMessageProperties.Expiration);
                }

                if (amqpMessageProperties.MessageCount.HasValue)
                {
                    headers.Add(AmqpHeaders.MESSAGE_COUNT, amqpMessageProperties.MessageCount.Value);
                }

                if (!string.IsNullOrEmpty(amqpMessageProperties.MessageId))
                {
                    headers.Add(AmqpHeaders.MESSAGE_ID, amqpMessageProperties.MessageId);
                }

                if (amqpMessageProperties.Priority.HasValue)
                {
                    headers.Add(AmqpMessageHeaderAccessor.PRIORITY, amqpMessageProperties.Priority.Value);
                }

                if (amqpMessageProperties.ReceivedDelay.HasValue)
                {
                    headers.Add(AmqpHeaders.RECEIVED_DELAY, amqpMessageProperties.ReceivedDelay.Value);
                }

                if (!string.IsNullOrEmpty(amqpMessageProperties.ReceivedExchange))
                {
                    headers.Add(AmqpHeaders.RECEIVED_EXCHANGE, amqpMessageProperties.ReceivedExchange);
                }

                if (!string.IsNullOrEmpty(amqpMessageProperties.ReceivedRoutingKey))
                {
                    headers.Add(AmqpHeaders.RECEIVED_ROUTING_KEY, amqpMessageProperties.ReceivedRoutingKey);
                }

                if (amqpMessageProperties.Redelivered.HasValue)
                {
                    headers.Add(AmqpHeaders.REDELIVERED, amqpMessageProperties.Redelivered.Value);
                }

                if (!string.IsNullOrEmpty(amqpMessageProperties.ReplyTo))
                {
                    headers.Add(AmqpHeaders.REPLY_TO, amqpMessageProperties.ReplyTo);
                }

                if (amqpMessageProperties.Timestamp.HasValue)
                {
                    headers.Add(AmqpHeaders.TIMESTAMP, amqpMessageProperties.Timestamp.Value);
                }

                if (!string.IsNullOrEmpty(amqpMessageProperties.Type))
                {
                    headers.Add(AmqpHeaders.TYPE, amqpMessageProperties.Type);
                }

                if (!string.IsNullOrEmpty(amqpMessageProperties.ReceivedUserId))
                {
                    headers.Add(AmqpHeaders.RECEIVED_USER_ID, amqpMessageProperties.ReceivedUserId);
                }

                if (!string.IsNullOrEmpty(amqpMessageProperties.ConsumerTag))
                {
                    headers.Add(AmqpHeaders.CONSUMER_TAG, amqpMessageProperties.ConsumerTag);
                }

                if (!string.IsNullOrEmpty(amqpMessageProperties.ConsumerQueue))
                {
                    headers.Add(AmqpHeaders.CONSUMER_QUEUE, amqpMessageProperties.ConsumerQueue);
                }

                headers.Add(AmqpHeaders.LAST_IN_BATCH, amqpMessageProperties.LastInBatch);

                foreach (var entry in amqpMessageProperties.Headers)
                {
                    headers[entry.Key] = entry.Value;
                }
            }
            catch (Exception e)
            {
                _logger?.LogWarning("Error occurred while mapping from AMQP properties to MessageHeaders", e);
            }

            return new MessageHeaders(headers);
        }

        private string ExtractContentTypeAsString(IDictionary<string, object> headers)
        {
            string contentTypeStringValue = null;

            var contentType = GetHeaderIfAvailable<object>(headers, AmqpHeaders.CONTENT_TYPE);

            if (contentType != null)
            {
                if (contentType is MimeType)
                {
                    contentTypeStringValue = contentType.ToString();
                }
                else if (contentType is string)
                {
                    contentTypeStringValue = (string)contentType;
                }
                else
                {
                    _logger?.LogWarning("skipping header '" + AmqpHeaders.CONTENT_TYPE +
                            "' since it is not of expected type [" + contentType.GetType().Name + "]");
                }
            }

            return contentTypeStringValue;
        }
    }
}
