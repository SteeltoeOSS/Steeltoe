// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;

namespace Steeltoe.Integration.Support;

public class IntegrationMessageBuilder<T> : IntegrationMessageBuilder, IMessageBuilder<T>
{
    internal IntegrationMessageBuilder(T payload, IMessage<T> originalMessage)
        : base(payload, originalMessage)
    {
    }

    public new T Payload => (T)base.Payload;

    public static IntegrationMessageBuilder<T> FromMessage(IMessage<T> message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        return new IntegrationMessageBuilder<T>(message.Payload, message);
    }

    public static IntegrationMessageBuilder<T> WithPayload(T payload)
    {
        return new IntegrationMessageBuilder<T>(payload, null);
    }

    public new IMessageBuilder<T> SetHeader(string headerName, object headerValue)
    {
        base.SetHeader(headerName, headerValue);
        return this;
    }

    public new IMessageBuilder<T> SetHeaderIfAbsent(string headerName, object headerValue)
    {
        base.SetHeaderIfAbsent(headerName, headerValue);
        return this;
    }

    public new IMessageBuilder<T> RemoveHeaders(params string[] headerPatterns)
    {
        base.RemoveHeaders(headerPatterns);
        return this;
    }

    public new IMessageBuilder<T> RemoveHeader(string headerName)
    {
        base.RemoveHeader(headerName);
        return this;
    }

    public new IMessageBuilder<T> CopyHeaders(IDictionary<string, object> headersToCopy)
    {
        base.CopyHeaders(headersToCopy);
        return this;
    }

    public new IMessageBuilder<T> CopyHeadersIfAbsent(IDictionary<string, object> headersToCopy)
    {
        base.CopyHeadersIfAbsent(headersToCopy);
        return this;
    }

    public new IMessageBuilder<T> ReadOnlyHeaders(IList<string> readOnlyHeaders)
    {
        base.ReadOnlyHeaders(readOnlyHeaders);
        return this;
    }

    public new IMessage<T> Build()
    {
        if (!modified && !HeaderAccessor.IsModified && OriginalMessage != null
            && !ContainsReadOnly(OriginalMessage.Headers))
        {
            return (IMessage<T>)OriginalMessage;
        }

        if (InnerPayload is Exception exception)
        {
            return (IMessage<T>)new ErrorMessage(exception, HeaderAccessor.ToDictionary());
        }

        return Message.Create((T)InnerPayload, HeaderAccessor.ToDictionary());
    }

    public new IMessageBuilder<T> SetExpirationDate(long expirationDate)
    {
        base.SetExpirationDate(expirationDate);
        return this;
    }

    public new IMessageBuilder<T> SetExpirationDate(DateTime? expirationDate)
    {
        base.SetExpirationDate(expirationDate);
        return this;
    }

    public new IMessageBuilder<T> SetCorrelationId(object correlationId)
    {
        base.SetCorrelationId(correlationId);
        return this;
    }

    public new IMessageBuilder<T> PushSequenceDetails(object correlationId, int sequenceNumber, int sequenceSize)
    {
        base.PushSequenceDetails(correlationId, sequenceNumber, sequenceSize);
        return this;
    }

    public new IMessageBuilder<T> PopSequenceDetails()
    {
        base.PopSequenceDetails();
        return this;
    }

    public new IMessageBuilder<T> SetReplyChannel(IMessageChannel replyChannel)
    {
        base.SetReplyChannel(replyChannel);
        return this;
    }

    public new IMessageBuilder<T> SetReplyChannelName(string replyChannelName)
    {
        base.SetReplyChannelName(replyChannelName);
        return this;
    }

    public new IMessageBuilder<T> SetErrorChannel(IMessageChannel errorChannel)
    {
        base.SetErrorChannel(errorChannel);
        return this;
    }

    public new IMessageBuilder<T> SetErrorChannelName(string errorChannelName)
    {
        base.SetErrorChannelName(errorChannelName);
        return this;
    }

    public new IMessageBuilder<T> SetSequenceNumber(int sequenceNumber)
    {
        base.SetSequenceNumber(sequenceNumber);
        return this;
    }

    public new IMessageBuilder<T> SetSequenceSize(int sequenceSize)
    {
        base.SetSequenceSize(sequenceSize);
        return this;
    }

    public new IMessageBuilder<T> SetPriority(int priority)
    {
        base.SetPriority(priority);
        return this;
    }

    public new IMessageBuilder<T> FilterAndCopyHeadersIfAbsent(IDictionary<string, object> headersToCopy, params string[] headerPatternsToFilter)
    {
        base.FilterAndCopyHeadersIfAbsent(headersToCopy, headerPatternsToFilter);
        return this;
    }
}

public class IntegrationMessageBuilder : AbstractMessageBuilder
{
    internal IntegrationMessageBuilder(object payload, IMessage originalMessage)
        : base(payload, originalMessage)
    {
    }

    public override object Payload => InnerPayload;

    public override IDictionary<string, object> Headers => HeaderAccessor.ToDictionary();

    public static IntegrationMessageBuilder FromMessage(IMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        return new IntegrationMessageBuilder(message.Payload, message);
    }

    public static IntegrationMessageBuilder WithPayload(object payload)
    {
        return new IntegrationMessageBuilder(payload, null);
    }

    public override IMessageBuilder SetHeader(string headerName, object headerValue)
    {
        HeaderAccessor.SetHeader(headerName, headerValue);
        return this;
    }

    public override IMessageBuilder SetHeaderIfAbsent(string headerName, object headerValue)
    {
        HeaderAccessor.SetHeaderIfAbsent(headerName, headerValue);
        return this;
    }

    public override IMessageBuilder RemoveHeaders(params string[] headerPatterns)
    {
        HeaderAccessor.RemoveHeaders(headerPatterns);
        return this;
    }

    public override IMessageBuilder RemoveHeader(string headerName)
    {
        if (!HeaderAccessor.IsReadOnly(headerName))
        {
            HeaderAccessor.RemoveHeader(headerName);
        }

        return this;
    }

    public override IMessageBuilder CopyHeaders(IDictionary<string, object> headersToCopy)
    {
        HeaderAccessor.CopyHeaders(headersToCopy);
        return this;
    }

    public override IMessageBuilder CopyHeadersIfAbsent(IDictionary<string, object> headersToCopy)
    {
        if (headersToCopy != null)
        {
            foreach (var entry in headersToCopy)
            {
                var headerName = entry.Key;
                if (!HeaderAccessor.IsReadOnly(headerName))
                {
                    HeaderAccessor.SetHeaderIfAbsent(headerName, entry.Value);
                }
            }
        }

        return this;
    }

    protected override List<List<object>> SequenceDetails => (List<List<object>>)HeaderAccessor.GetHeader(IntegrationMessageHeaderAccessor.SequenceDetails);

    protected override object CorrelationId => HeaderAccessor.GetCorrelationId();

    protected override object SequenceNumber => HeaderAccessor.GetSequenceNumber();

    protected override object SequenceSize => HeaderAccessor.GetSequenceSize();

    public IMessageBuilder ReadOnlyHeaders(IList<string> readOnlyHeaders)
    {
        base.readOnlyHeaders = readOnlyHeaders;
        HeaderAccessor.SetReadOnlyHeaders(readOnlyHeaders);
        return this;
    }

    public override IMessage Build()
    {
        if (!modified && !HeaderAccessor.IsModified && OriginalMessage != null
            && !ContainsReadOnly(OriginalMessage.Headers))
        {
            return OriginalMessage;
        }

        if (InnerPayload is Exception exception)
        {
            return new ErrorMessage(exception, HeaderAccessor.ToDictionary());
        }

        return Message.Create(InnerPayload, HeaderAccessor.ToDictionary(), InnerPayload.GetType());
    }
}
