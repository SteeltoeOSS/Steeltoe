// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Converter;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Core;

public abstract class AbstractMessageSendingTemplate<D> : IMessageSendingOperations<D>
{
    public const string CONVERSION_HINT_HEADER = "conversionHint";

    public virtual D DefaultSendDestination { get; set; }

    public virtual IMessageConverter MessageConverter { get; set; } = new SimpleMessageConverter();

    public virtual Task ConvertAndSendAsync(object payload, CancellationToken cancellationToken = default)
    {
        return ConvertAndSendAsync(payload, null, cancellationToken);
    }

    public virtual Task ConvertAndSendAsync(D destination, object payload, CancellationToken cancellationToken = default)
    {
        return ConvertAndSendAsync(destination, payload, (IDictionary<string, object>)null, cancellationToken);
    }

    public virtual Task ConvertAndSendAsync(D destination, object payload, IDictionary<string, object> headers, CancellationToken cancellationToken = default)
    {
        return ConvertAndSendAsync(destination, payload, headers, null, cancellationToken);
    }

    public virtual Task ConvertAndSendAsync(object payload, IMessagePostProcessor postProcessor, CancellationToken cancellationToken = default)
    {
        return ConvertAndSendAsync(RequiredDefaultSendDestination, payload, postProcessor, cancellationToken);
    }

    public virtual Task ConvertAndSendAsync(D destination, object payload, IMessagePostProcessor postProcessor, CancellationToken cancellationToken = default)
    {
        return ConvertAndSendAsync(destination, payload, null, postProcessor, cancellationToken);
    }

    public virtual Task ConvertAndSendAsync(D destination, object payload, IDictionary<string, object> headers, IMessagePostProcessor postProcessor, CancellationToken cancellationToken = default)
    {
        var message = DoConvert(payload, headers, postProcessor);
        return SendAsync(destination, message, cancellationToken);
    }

    public virtual Task SendAsync(IMessage message, CancellationToken cancellationToken = default)
    {
        return SendAsync(RequiredDefaultSendDestination, message, cancellationToken);
    }

    public virtual Task SendAsync(D destination, IMessage message, CancellationToken cancellationToken = default)
    {
        return DoSendAsync(destination, message, cancellationToken);
    }

    public virtual void ConvertAndSend(object payload)
    {
        ConvertAndSend(payload, null);
    }

    public virtual void ConvertAndSend(D destination, object payload)
    {
        ConvertAndSend(destination, payload, (IDictionary<string, object>)null);
    }

    public virtual void ConvertAndSend(D destination, object payload, IDictionary<string, object> headers)
    {
        ConvertAndSend(destination, payload, headers, null);
    }

    public virtual void ConvertAndSend(object payload, IMessagePostProcessor postProcessor)
    {
        ConvertAndSend(RequiredDefaultSendDestination, payload, postProcessor);
    }

    public virtual void ConvertAndSend(D destination, object payload, IMessagePostProcessor postProcessor)
    {
        ConvertAndSend(destination, payload, null, postProcessor);
    }

    public virtual void ConvertAndSend(D destination, object payload, IDictionary<string, object> headers, IMessagePostProcessor postProcessor)
    {
        var message = DoConvert(payload, headers, postProcessor);
        Send(destination, message);
    }

    public virtual void Send(IMessage message)
    {
        Send(RequiredDefaultSendDestination, message);
    }

    public virtual void Send(D destination, IMessage message)
    {
        DoSend(destination, message);
    }

    protected abstract Task DoSendAsync(D destination, IMessage message, CancellationToken cancellationToken);

    protected abstract void DoSend(D destination, IMessage message);

    protected virtual D RequiredDefaultSendDestination
    {
        get => DefaultSendDestination ?? throw new InvalidOperationException("No default destination configured");
    }

    protected virtual IMessage DoConvert(object payload, IDictionary<string, object> headers, IMessagePostProcessor postProcessor)
    {
        IMessageHeaders messageHeaders = null;
        object conversionHint = null;
        headers?.TryGetValue(CONVERSION_HINT_HEADER, out conversionHint);

        var headersToUse = ProcessHeadersToSend(headers);
        if (headersToUse != null)
        {
            if (headersToUse is MessageHeaders headers1)
            {
                messageHeaders = headers1;
            }
            else
            {
                messageHeaders = new MessageHeaders(headersToUse, null, null);
            }
        }

        var converter = MessageConverter;
        var message = converter is ISmartMessageConverter smartConverter ?
            smartConverter.ToMessage(payload, messageHeaders, conversionHint) :
            converter.ToMessage(payload, messageHeaders);
        if (message == null)
        {
            var payloadType = payload.GetType().Name;

            object contentType = null;
            messageHeaders?.TryGetValue(MessageHeaders.CONTENT_TYPE, out contentType);
            contentType ??= "unknown";

            throw new MessageConversionException("Unable to convert payload with type='" + payloadType +
                                                 "', contentType='" + contentType + "', converter=[" + MessageConverter + "]");
        }

        if (postProcessor != null)
        {
            message = postProcessor.PostProcessMessage(message);
        }

        return message;
    }

    protected virtual IDictionary<string, object> ProcessHeadersToSend(IDictionary<string, object> headers)
    {
        return headers;
    }
}