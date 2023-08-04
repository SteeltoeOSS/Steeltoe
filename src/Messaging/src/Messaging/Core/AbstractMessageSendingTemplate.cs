// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Steeltoe.Messaging.Converter;

namespace Steeltoe.Messaging.Core;

public abstract class AbstractMessageSendingTemplate<TDestination> : IMessageSendingOperations<TDestination>
{
    public const string ConversionHintHeader = "conversionHint";

    protected virtual TDestination RequiredDefaultSendDestination =>
        DefaultSendDestination ?? throw new InvalidOperationException("No default destination configured");

    public virtual TDestination DefaultSendDestination { get; set; }

    public virtual IMessageConverter MessageConverter { get; set; } = new SimpleMessageConverter();

    public virtual Task ConvertAndSendAsync(object payload, CancellationToken cancellationToken)
    {
        return ConvertAndSendAsync(payload, null, cancellationToken);
    }
    public Task ConvertAndSendAsync(object payload)
    {
        return ConvertAndSendAsync(payload, null, default);
    }

    public virtual Task ConvertAndSendAsync(TDestination destination, object payload, CancellationToken cancellationToken)
    {
        return ConvertAndSendAsync(destination, payload, (IDictionary<string, object>)null, cancellationToken);
    }
    public Task ConvertAndSendAsync(TDestination destination, object payload)
    {
        return ConvertAndSendAsync(destination, payload, (IDictionary<string, object>)null, default(CancellationToken));
    }

    public virtual Task ConvertAndSendAsync(TDestination destination, object payload, IDictionary<string, object> headers,
        CancellationToken cancellationToken)
    {
        return ConvertAndSendAsync(destination, payload, headers, null, cancellationToken);
    }
    public Task ConvertAndSendAsync(TDestination destination, object payload, IDictionary<string, object> headers)
    {
        return ConvertAndSendAsync(destination, payload, headers, null, default);
    }
    public virtual Task ConvertAndSendAsync(object payload, IMessagePostProcessor postProcessor, CancellationToken cancellationToken)
    {
        return ConvertAndSendAsync(RequiredDefaultSendDestination, payload, postProcessor);
    }
    public Task ConvertAndSendAsync(object payload, IMessagePostProcessor postProcessor)
    {
        return ConvertAndSendAsync(RequiredDefaultSendDestination, payload, postProcessor);
    }
    public virtual Task ConvertAndSendAsync(TDestination destination, object payload, IMessagePostProcessor postProcessor)
    {
        return ConvertAndSendAsync(destination, payload, null, postProcessor, default);
    }
    public Task ConvertAndSendAsync(TDestination destination, object payload, IMessagePostProcessor postProcessor, CancellationToken cancellationToken)
    {
        return ConvertAndSendAsync(destination, payload, null, postProcessor, cancellationToken);
    }
    public virtual Task ConvertAndSendAsync(TDestination destination, object payload, IDictionary<string, object> headers, IMessagePostProcessor postProcessor)
    {
       return ConvertAndSendAsync(destination, payload, headers, postProcessor, default);
    }
    public virtual Task ConvertAndSendAsync(TDestination destination, object payload, IDictionary<string, object> headers, IMessagePostProcessor postProcessor,
        CancellationToken cancellationToken = default)
    {
        IMessage message = DoConvert(payload, headers, postProcessor);
        return SendAsync(destination, message, cancellationToken);
    }

    public virtual Task SendAsync(IMessage message, CancellationToken cancellationToken)
    {
        return SendAsync(RequiredDefaultSendDestination, message, cancellationToken);
    }
    public Task SendAsync(IMessage message)
    {
        return SendAsync(RequiredDefaultSendDestination, message, default);
    }
    public virtual Task SendAsync(TDestination destination, IMessage message, CancellationToken cancellationToken )
    {
        return DoSendAsync(destination, message, cancellationToken);
    }
    public Task SendAsync(TDestination destination, IMessage message)
    {
        return DoSendAsync(destination, message, default);
    }

    public virtual void ConvertAndSend(object payload)
    {
        ConvertAndSend(payload, null);
    }

    public virtual void ConvertAndSend(TDestination destination, object payload)
    {
        ConvertAndSend(destination, payload, (IDictionary<string, object>)null);
    }

    public virtual void ConvertAndSend(TDestination destination, object payload, IDictionary<string, object> headers)
    {
        ConvertAndSend(destination, payload, headers, null);
    }

    public virtual void ConvertAndSend(object payload, IMessagePostProcessor postProcessor)
    {
        ConvertAndSend(RequiredDefaultSendDestination, payload, postProcessor);
    }

    public virtual void ConvertAndSend(TDestination destination, object payload, IMessagePostProcessor postProcessor)
    {
        ConvertAndSend(destination, payload, null, postProcessor);
    }

    public virtual void ConvertAndSend(TDestination destination, object payload, IDictionary<string, object> headers, IMessagePostProcessor postProcessor)
    {
        IMessage message = DoConvert(payload, headers, postProcessor);
        Send(destination, message);
    }

    public virtual void Send(IMessage message)
    {
        Send(RequiredDefaultSendDestination, message);
    }

    public virtual void Send(TDestination destination, IMessage message)
    {
        DoSend(destination, message);
    }

    protected abstract Task DoSendAsync(TDestination destination, IMessage message, CancellationToken cancellationToken);

    protected abstract void DoSend(TDestination destination, IMessage message);

    protected virtual IMessage DoConvert(object payload, IDictionary<string, object> headers, IMessagePostProcessor postProcessor)
    {
        IMessageHeaders messageHeaders = null;
        object conversionHint = null;
        headers?.TryGetValue(ConversionHintHeader, out conversionHint);

        IDictionary<string, object> headersToUse = ProcessHeadersToSend(headers);

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

        IMessageConverter converter = MessageConverter;

        IMessage message = converter is ISmartMessageConverter smartConverter
            ? smartConverter.ToMessage(payload, messageHeaders, conversionHint)
            : converter.ToMessage(payload, messageHeaders);

        if (message == null)
        {
            string payloadType = payload.GetType().Name;

            object contentType = null;
            messageHeaders?.TryGetValue(MessageHeaders.ContentType, out contentType);
            contentType ??= "unknown";

            throw new MessageConversionException(
                $"Unable to convert payload with type='{payloadType}', contentType='{contentType}', converter=[{MessageConverter}]");
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
