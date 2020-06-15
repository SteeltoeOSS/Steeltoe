// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
