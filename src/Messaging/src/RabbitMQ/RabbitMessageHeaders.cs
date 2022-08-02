// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ;

public static class RabbitMessageHeaders
{
    public const string Prefix = "amqp_";
    public const string RabbitProperty = "rabbit_";

    // Encoded in Rabbit Message properties
    public const string AppId = $"{RabbitProperty}appId";
    public const string ClusterId = $"{RabbitProperty}clusterId";
    public const string ContentEncoding = $"{RabbitProperty}contentEncoding";
    public const string CorrelationId = $"{RabbitProperty}correlationId";
    public const string Delay = $"{RabbitProperty}delay";
    public const string DeliveryMode = $"{RabbitProperty}deliveryMode";
    public const string DeliveryTag = $"{RabbitProperty}deliveryTag";
    public const string Expiration = $"{RabbitProperty}expiration";
    public const string MessageId = $"{RabbitProperty}messageId";
    public const string ReceivedExchange = $"{RabbitProperty}receivedExchange";
    public const string ReceivedRoutingKey = $"{RabbitProperty}receivedRoutingKey";
    public const string Redelivered = $"{RabbitProperty}redelivered";
    public const string ReplyTo = $"{RabbitProperty}replyTo";
    public const string Timestamp = $"{RabbitProperty}timestamp";
    public const string Type = $"{RabbitProperty}type";
    public const string UserId = $"{RabbitProperty}userId";
    public const string ReceivedDeliveryMode = $"{RabbitProperty}receivedDeliveryMode";
    public const string ReceivedUserId = $"{RabbitProperty}receivedUserId";
    public const string ReceivedDelay = $"{RabbitProperty}receivedDelay";
    public const string Priority = $"{RabbitProperty}priority";

    public const string ContentType = MessageHeaders.ContentType;
    public const string ContentLength = $"{MessageHeaders.Internal}contentLength";
    public const string MessageCount = $"{MessageHeaders.Internal}messageCount";
    public const string PublishSequenceNumber = $"{MessageHeaders.Internal}PublishSequenceNumber";

    public const string FinalRetryForMessageWithNoId = $"{MessageHeaders.Internal}FinalRetryForMessageWithNoID";

    // Used in RabbitMQ Integration code
    public const string PublishConfirmCorrelation = $"{Prefix}_publishConfirmCorrelation";
    public const string PublishConfirm = $"{Prefix}publishConfirm";
    public const string PublishConfirmNackCause = $"{Prefix}publishConfirmNackCause";
    public const string ReturnReplyCode = $"{Prefix}returnReplyCode";
    public const string ReturnReplyText = $"{Prefix}returnReplyText";
    public const string ReturnExchange = $"{Prefix}returnExchange";
    public const string ReturnRoutingKey = $"{Prefix}returnRoutingKey";
    public const string RawMessage = $"{Prefix}raw_message";

    // Used and found in AMQPHeaders
    public const string Channel = $"{MessageHeaders.Internal}channel";
    public const string ConsumerTag = $"{MessageHeaders.Internal}consumerTag";
    public const string ConsumerQueue = $"{MessageHeaders.Internal}consumerQueue";

    public const string LastInBatch = $"{Prefix}lastInBatch";
    public const string BatchSize = $"{Prefix}batchSize";

    public const string Target = $"{MessageHeaders.Internal}Target";
    public const string TargetMethod = $"{MessageHeaders.Internal}TargetMethod";

    public const string SpringBatchFormat = "springBatchFormat";
    public const string SpringReplyCorrelation = "spring_reply_correlation";
    public const string SpringReplyToStack = "spring_reply_to";
    public const string BatchFormatLengthHeader4 = "lengthHeader4";
    public const string SpringAutoDecompress = "springAutoDecompress";
    public const string XDelay = "x-delay";
    public const string XDeath = "x-death";
}
