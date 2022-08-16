// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using Steeltoe.Common.Util;
using Steeltoe.Messaging;

namespace Steeltoe.Integration.Support;

public abstract class AbstractMessageBuilder : IMessageBuilder
{
    protected readonly object InnerPayload;

    protected readonly IMessage OriginalMessage;

    protected readonly IntegrationMessageHeaderAccessor HeaderAccessor;

    protected volatile bool modified;

    protected IList<string> readOnlyHeaders;

#pragma warning disable S3956 // "Generic.List" instances should not be part of public APIs
    protected abstract List<List<object>> SequenceDetails { get; }
#pragma warning restore S3956 // "Generic.List" instances should not be part of public APIs

    protected abstract object CorrelationId { get; }

    protected abstract object SequenceNumber { get; }

    protected abstract object SequenceSize { get; }

    public abstract object Payload { get; }

    public abstract IDictionary<string, object> Headers { get; }

    protected AbstractMessageBuilder()
    {
    }

    protected AbstractMessageBuilder(object payload, IMessage originalMessage)
    {
        ArgumentGuard.NotNull(payload);

        InnerPayload = payload;
        OriginalMessage = originalMessage;
        HeaderAccessor = new IntegrationMessageHeaderAccessor(originalMessage);

        if (originalMessage != null)
        {
            modified = !InnerPayload.Equals(originalMessage.Payload);
        }
    }

    public virtual IMessageBuilder SetExpirationDate(long expirationDate)
    {
        return SetHeader(IntegrationMessageHeaderAccessor.ExpirationDate, expirationDate);
    }

    public virtual IMessageBuilder SetExpirationDate(DateTime? expirationDate)
    {
        if (expirationDate != null)
        {
            var datetime = new DateTimeOffset(expirationDate.Value);
            return SetHeader(IntegrationMessageHeaderAccessor.ExpirationDate, datetime.ToUnixTimeMilliseconds());
        }

        return SetHeader(IntegrationMessageHeaderAccessor.ExpirationDate, null);
    }

    public virtual IMessageBuilder SetCorrelationId(object correlationId)
    {
        return SetHeader(IntegrationMessageHeaderAccessor.CorrelationId, correlationId);
    }

    public virtual IMessageBuilder PushSequenceDetails(object correlationId, int sequenceNumber, int sequenceSize)
    {
        object incomingCorrelationId = CorrelationId;
        List<List<object>> incomingSequenceDetails = SequenceDetails;

        if (incomingCorrelationId != null)
        {
            incomingSequenceDetails = incomingSequenceDetails == null ? new List<List<object>>() : new List<List<object>>(incomingSequenceDetails);

            incomingSequenceDetails.Add(new List<object>
            {
                incomingCorrelationId,
                SequenceNumber,
                SequenceSize
            });
        }

        if (incomingSequenceDetails != null)
        {
            SetHeader(IntegrationMessageHeaderAccessor.SequenceDetails, incomingSequenceDetails);
        }

        return SetCorrelationId(correlationId).SetSequenceNumber(sequenceNumber).SetSequenceSize(sequenceSize);
    }

    public virtual IMessageBuilder PopSequenceDetails()
    {
        List<List<object>> incomingSequenceDetails = SequenceDetails;

        if (incomingSequenceDetails == null)
        {
            return this;
        }

        incomingSequenceDetails = new List<List<object>>(incomingSequenceDetails);

        List<object> sequenceDetails = incomingSequenceDetails[incomingSequenceDetails.Count - 1];
        incomingSequenceDetails.RemoveAt(incomingSequenceDetails.Count - 1);

        if (sequenceDetails.Count != 3)
        {
            throw new InvalidOperationException("Wrong sequence details (not created by MessageBuilder?)");
        }

        SetCorrelationId(sequenceDetails[0]);
        int? sequenceNumber = sequenceDetails[1] as int?;
        int? sequenceSize = sequenceDetails[2] as int?;

        if (sequenceNumber.HasValue)
        {
            SetSequenceNumber(sequenceNumber.Value);
        }

        if (sequenceSize.HasValue)
        {
            SetSequenceSize(sequenceSize.Value);
        }

        if (incomingSequenceDetails.Count > 0)
        {
            SetHeader(IntegrationMessageHeaderAccessor.SequenceDetails, incomingSequenceDetails);
        }
        else
        {
            RemoveHeader(IntegrationMessageHeaderAccessor.SequenceDetails);
        }

        return this;
    }

    public virtual IMessageBuilder SetReplyChannel(IMessageChannel replyChannel)
    {
        return SetHeader(MessageHeaders.ReplyChannelName, replyChannel);
    }

    public virtual IMessageBuilder SetReplyChannelName(string replyChannelName)
    {
        return SetHeader(MessageHeaders.ReplyChannelName, replyChannelName);
    }

    public virtual IMessageBuilder SetErrorChannel(IMessageChannel errorChannel)
    {
        return SetHeader(MessageHeaders.ErrorChannelName, errorChannel);
    }

    public virtual IMessageBuilder SetErrorChannelName(string errorChannelName)
    {
        return SetHeader(MessageHeaders.ErrorChannelName, errorChannelName);
    }

    public virtual IMessageBuilder SetSequenceNumber(int sequenceNumber)
    {
        return SetHeader(IntegrationMessageHeaderAccessor.SequenceNumber, sequenceNumber);
    }

    public virtual IMessageBuilder SetSequenceSize(int sequenceSize)
    {
        return SetHeader(IntegrationMessageHeaderAccessor.SequenceSize, sequenceSize);
    }

    public virtual IMessageBuilder SetPriority(int priority)
    {
        return SetHeader(IntegrationMessageHeaderAccessor.Priority, priority);
    }

    public virtual IMessageBuilder FilterAndCopyHeadersIfAbsent(IDictionary<string, object> headersToCopy, params string[] headerPatternsToFilter)
    {
        IDictionary<string, object> headers = new Dictionary<string, object>(headersToCopy);

        if (headerPatternsToFilter?.Length > 0)
        {
            foreach (KeyValuePair<string, object> entry in headersToCopy)
            {
                if (PatternMatchUtils.SimpleMatch(headerPatternsToFilter, entry.Key))
                {
                    headers.Remove(entry.Key);
                }
            }
        }

        return CopyHeadersIfAbsent(headers);
    }

    public abstract IMessageBuilder SetHeader(string headerName, object headerValue);

    public abstract IMessageBuilder SetHeaderIfAbsent(string headerName, object headerValue);

    public abstract IMessageBuilder RemoveHeaders(params string[] headerPatterns);

    public abstract IMessageBuilder RemoveHeader(string headerName);

    public abstract IMessageBuilder CopyHeaders(IDictionary<string, object> headersToCopy);

    public abstract IMessageBuilder CopyHeadersIfAbsent(IDictionary<string, object> headersToCopy);

    public abstract IMessage Build();

    protected bool ContainsReadOnly(IMessageHeaders headers)
    {
        if (readOnlyHeaders != null)
        {
            foreach (string readOnly in readOnlyHeaders)
            {
                if (headers.ContainsKey(readOnly))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
