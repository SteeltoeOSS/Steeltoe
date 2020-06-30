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

namespace Steeltoe.Messaging.Rabbit
{
    public static class RabbitMessageHeaders
    {
        public const string PREFIX = "amqp_";
        public const string RABBIT_PROPERTY = "rabbit_";
        public const string INTERNAL = "internal_amqp_";

        // Encoded in Rabbit Message properties
        public const string APP_ID = RABBIT_PROPERTY + "appId";// //
        public const string CLUSTER_ID = RABBIT_PROPERTY + "clusterId";// //
        public const string CONTENT_ENCODING = RABBIT_PROPERTY + "contentEncoding";// //
        public const string CORRELATION_ID = RABBIT_PROPERTY + "correlationId";// //
        public const string DELIVERY_MODE = RABBIT_PROPERTY + "deliveryMode";// Nulled on received //
        public const string DELIVERY_TAG = RABBIT_PROPERTY + "deliveryTag";//
        public const string EXPIRATION = RABBIT_PROPERTY + "expiration";// //
        public const string MESSAGE_ID = RABBIT_PROPERTY + "messageId";// //
        public const string RECEIVED_EXCHANGE = RABBIT_PROPERTY + "receivedExchange";//
        public const string RECEIVED_ROUTING_KEY = RABBIT_PROPERTY + "receivedRoutingKey";//
        public const string REDELIVERED = RABBIT_PROPERTY + "redelivered";//
        public const string REPLY_TO = RABBIT_PROPERTY + "replyTo";// //
        public const string TIMESTAMP = RABBIT_PROPERTY + "timestamp";// //
        public const string TYPE = RABBIT_PROPERTY + "type";// //
        public const string USER_ID = RABBIT_PROPERTY + "userId";// sent
        public const string RECEIVED_DELIVERY_MODE = RABBIT_PROPERTY + "receivedDeliveryMode";//
        public const string RECEIVED_USER_ID = RABBIT_PROPERTY + "receivedUserId"; // received
        public const string RECEIVED_DELAY = RABBIT_PROPERTY + "receivedDelay"; // Comes from X-DELAY
        public const string PRIORITY = RABBIT_PROPERTY + "priority";  // //
        public const string CONTENT_TYPE = MessageHeaders.CONTENT_TYPE; // //

        public const string CONTENT_LENGTH = INTERNAL + "contentLength";
        public const string MESSAGE_COUNT = INTERNAL + "messageCount";
        public const string PUBLISH_SEQUENCE_NUMBER = INTERNAL + "PublishSequenceNumber";
        public const string FINAL_RETRY_FOR_MESSAGE_WITH_NO_ID = INTERNAL + "FinalRetryForMessageWithNoID";

        // Not Used ... but found in AMQPHeaders
        public const string PUBLISH_CONFIRM = PREFIX + "publishConfirm";
        public const string PUBLISH_CONFIRM_NACK_CAUSE = PREFIX + "publishConfirmNackCause";
        public const string RETURN_REPLY_CODE = PREFIX + "returnReplyCode";
        public const string RETURN_REPLY_TEXT = PREFIX + "returnReplyText";
        public const string RETURN_EXCHANGE = PREFIX + "returnExchange";
        public const string RETURN_ROUTING_KEY = PREFIX + "returnRoutingKey";
        public const string RAW_MESSAGE = PREFIX + "raw_message";

        // Used and found in AMQPHeaders
        public const string CHANNEL = INTERNAL + "channel";
        public const string CONSUMER_TAG = INTERNAL + "consumerTag";
        public const string CONSUMER_QUEUE = INTERNAL + "consumerQueue";

        public const string LAST_IN_BATCH = PREFIX + "lastInBatch";
        public const string BATCH_SIZE = PREFIX + "batchSize";

        public const string INFERRED_ARGUMENT_TYPE = INTERNAL + MessageHeaders.INFERRED_ARGUMENT_TYPE;

        public const string TARGET = INTERNAL + "Target";
        public const string TARGET_METHOD = INTERNAL + "TargetMethod";


        public const string SPRING_BATCH_FORMAT = "springBatchFormat";
        public const string SPRING_REPLY_CORRELATION = "spring_reply_correlation";
        public const string SPRING_REPLY_TO_STACK = "spring_reply_to";
        public const string BATCH_FORMAT_LENGTH_HEADER4 = "lengthHeader4";
        public const string SPRING_AUTO_DECOMPRESS = "springAutoDecompress";
        public const string X_DELAY = "x-delay";
        public const string X_DEATH = "x-death";

        //public const string PRIORITY = "priority";
        //public const string CONTENT_TYPE = MessageHeaders.CONTENT_TYPE;
    }
}
