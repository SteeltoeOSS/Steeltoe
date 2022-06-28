// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Support;

public abstract class AbstractMessageBuilder
{
    protected readonly object payload;

    protected readonly IMessage originalMessage;

    protected MessageHeaderAccessor headerAccessor;

    protected AbstractMessageBuilder()
    {
    }

    protected AbstractMessageBuilder(IMessage message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        payload = message.Payload;
        originalMessage = message;
        headerAccessor = new MessageHeaderAccessor(message);
    }

    protected AbstractMessageBuilder(MessageHeaderAccessor accessor)
    {
        payload = null;
        originalMessage = null;
        headerAccessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }

    protected AbstractMessageBuilder(object payload, MessageHeaderAccessor accessor)
    {
        this.payload = payload ?? throw new ArgumentNullException(nameof(payload));
        originalMessage = null;
        headerAccessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }

    public abstract AbstractMessageBuilder SetHeaders(MessageHeaderAccessor accessor);

    public abstract AbstractMessageBuilder SetHeader(string headerName, object headerValue);

    public abstract AbstractMessageBuilder SetHeaderIfAbsent(string headerName, object headerValue);

    public abstract AbstractMessageBuilder RemoveHeaders(params string[] headerPatterns);

    public abstract AbstractMessageBuilder RemoveHeader(string headerName);

    public abstract AbstractMessageBuilder CopyHeaders(IDictionary<string, object> headersToCopy);

    public abstract AbstractMessageBuilder CopyHeadersIfAbsent(IDictionary<string, object> headersToCopy);

    public abstract AbstractMessageBuilder SetReplyChannel(IMessageChannel replyChannel);

    public abstract AbstractMessageBuilder SetReplyChannelName(string replyChannelName);

    public abstract AbstractMessageBuilder SetErrorChannel(IMessageChannel errorChannel);

    public abstract AbstractMessageBuilder SetErrorChannelName(string errorChannelName);

    public virtual IMessage Build()
    {
        if (originalMessage != null && !headerAccessor.IsModified)
        {
            return originalMessage;
        }

        var headersToUse = headerAccessor.ToMessageHeaders();
        return Message.Create(payload, headersToUse, payload.GetType());
    }
}
