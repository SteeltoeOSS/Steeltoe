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

namespace Steeltoe.Messaging.Rabbit.Support
{
    public static class AmqpHeaders
    {
        public const string PREFIX = "amqp_";
        public const string APP_ID = PREFIX + "appId";
        public const string CLUSTER_ID = PREFIX + "clusterId";
        public const string CONTENT_ENCODING = PREFIX + "contentEncoding";
        public const string CONTENT_LENGTH = PREFIX + "contentLength";
        public const string CONTENT_TYPE = MessageHeaders.CONTENT_TYPE;
        public const string CORRELATION_ID = PREFIX + "correlationId";
        public const string DELAY = PREFIX + "delay";
        public const string DELIVERY_MODE = PREFIX + "deliveryMode";
        public const string DELIVERY_TAG = PREFIX + "deliveryTag";
        public const string EXPIRATION = PREFIX + "expiration";
        public const string MESSAGE_COUNT = PREFIX + "messageCount";
        public const string MESSAGE_ID = PREFIX + "messageId";
        public const string RECEIVED_DELAY = PREFIX + "receivedDelay";
        public const string RECEIVED_DELIVERY_MODE = PREFIX + "receivedDeliveryMode";
        public const string RECEIVED_EXCHANGE = PREFIX + "receivedExchange";
        public const string RECEIVED_ROUTING_KEY = PREFIX + "receivedRoutingKey";
        public const string RECEIVED_USER_ID = PREFIX + "receivedUserId";
        public const string REDELIVERED = PREFIX + "redelivered";
        public const string REPLY_TO = PREFIX + "replyTo";
        public const string TIMESTAMP = PREFIX + "timestamp";
        public const string TYPE = PREFIX + "type";
        public const string USER_ID = PREFIX + "userId";
        public const string SPRING_REPLY_CORRELATION = PREFIX + "springReplyCorrelation";
        public const string SPRING_REPLY_TO_STACK = PREFIX + "springReplyToStack";
        public const string PUBLISH_CONFIRM = PREFIX + "publishConfirm";
        public const string PUBLISH_CONFIRM_NACK_CAUSE = PREFIX + "publishConfirmNackCause";
        public const string RETURN_REPLY_CODE = PREFIX + "returnReplyCode";
        public const string RETURN_REPLY_TEXT = PREFIX + "returnReplyText";
        public const string RETURN_EXCHANGE = PREFIX + "returnExchange";
        public const string RETURN_ROUTING_KEY = PREFIX + "returnRoutingKey";
        public const string CHANNEL = PREFIX + "channel";
        public const string CONSUMER_TAG = PREFIX + "consumerTag";
        public const string CONSUMER_QUEUE = PREFIX + "consumerQueue";
        public const string RAW_MESSAGE = PREFIX + "raw_message";
        public const string LAST_IN_BATCH = PREFIX + "lastInBatch";
        public const string BATCH_SIZE = PREFIX + "batchSize";
    }
}
