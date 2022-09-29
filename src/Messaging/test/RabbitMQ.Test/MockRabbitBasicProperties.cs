// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ;

public class MockRabbitBasicProperties : RC.IBasicProperties
{
    int RC.IContentHeader.ProtocolClassId => 0;

    string RC.IContentHeader.ProtocolClassName => string.Empty;

    public ushort ProtocolClassId { get; }

    public string ProtocolClassName { get; }

    public string AppId { get; set; }

    public string ClusterId { get; set; }

    public string ContentEncoding { get; set; }

    public string ContentType { get; set; }

    public string CorrelationId { get; set; }

    public byte DeliveryMode { get; set; } = 0xff;

    public string Expiration { get; set; }

    public IDictionary<string, object> Headers { get; set; } = new Dictionary<string, object>();

    public string MessageId { get; set; }

    public bool Persistent { get; set; }

    public byte Priority { get; set; } = 0xff;

    public string ReplyTo { get; set; }

    public RC.PublicationAddress ReplyToAddress { get; set; }

    public RC.AmqpTimestamp Timestamp { get; set; }

    public string Type { get; set; }

    public string UserId { get; set; }

    public void ClearAppId()
    {
    }

    public void ClearClusterId()
    {
    }

    public void ClearContentEncoding()
    {
    }

    public void ClearContentType()
    {
    }

    public void ClearCorrelationId()
    {
    }

    public void ClearDeliveryMode()
    {
    }

    public void ClearExpiration()
    {
    }

    public void ClearHeaders()
    {
    }

    public void ClearMessageId()
    {
    }

    public void ClearPriority()
    {
    }

    public void ClearReplyTo()
    {
    }

    public void ClearTimestamp()
    {
    }

    public void ClearType()
    {
    }

    public void ClearUserId()
    {
    }

    public bool IsAppIdPresent()
    {
        return AppId != null;
    }

    public bool IsClusterIdPresent()
    {
        return ClusterId != null;
    }

    public bool IsContentEncodingPresent()
    {
        return ContentEncoding != null;
    }

    public bool IsContentTypePresent()
    {
        return ContentType != null;
    }

    public bool IsCorrelationIdPresent()
    {
        return CorrelationId != null;
    }

    public bool IsDeliveryModePresent()
    {
        return DeliveryMode != 0xff;
    }

    public bool IsExpirationPresent()
    {
        return Expiration != null;
    }

    public bool IsHeadersPresent()
    {
        return Headers != null;
    }

    public bool IsMessageIdPresent()
    {
        return MessageId != null;
    }

    public bool IsPriorityPresent()
    {
        return Priority != 0xff;
    }

    public bool IsReplyToPresent()
    {
        return ReplyTo != null;
    }

    public bool IsTimestampPresent()
    {
        return !default(RC.AmqpTimestamp).Equals(Timestamp);
    }

    public bool IsTypePresent()
    {
        return Type != null;
    }

    public bool IsUserIdPresent()
    {
        return UserId != null;
    }

    public void SetPersistent(bool persistent)
    {
        Persistent = persistent;
    }
}
