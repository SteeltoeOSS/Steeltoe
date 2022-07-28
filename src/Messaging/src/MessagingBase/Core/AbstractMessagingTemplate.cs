// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Core;

public abstract class AbstractMessagingTemplate<TDestination>
    : AbstractMessageReceivingTemplate<TDestination>,
        IMessageRequestReplyOperations<TDestination>
{
    public virtual Task<T> ConvertSendAndReceiveAsync<T>(object request, CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsync<T>(RequiredDefaultSendDestination, request, cancellationToken);
    }

    public virtual Task<T> ConvertSendAndReceiveAsync<T>(TDestination destination, object request, CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsync<T>(destination, request, (IDictionary<string, object>)null, cancellationToken);
    }

    public virtual Task<T> ConvertSendAndReceiveAsync<T>(TDestination destination, object request, IDictionary<string, object> headers, CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsync<T>(destination, request, headers, null, cancellationToken);
    }

    public virtual Task<T> ConvertSendAndReceiveAsync<T>(object request, IMessagePostProcessor requestPostProcessor, CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsync<T>(RequiredDefaultSendDestination, request, requestPostProcessor, cancellationToken);
    }

    public virtual Task<T> ConvertSendAndReceiveAsync<T>(TDestination destination, object request, IMessagePostProcessor requestPostProcessor, CancellationToken cancellationToken = default)
    {
        return ConvertSendAndReceiveAsync<T>(destination, request, null, requestPostProcessor, cancellationToken);
    }

    public virtual async Task<T> ConvertSendAndReceiveAsync<T>(TDestination destination, object request, IDictionary<string, object> headers, IMessagePostProcessor requestPostProcessor, CancellationToken cancellationToken = default)
    {
        var requestMessage = DoConvert(request, headers, requestPostProcessor);
        var replyMessage = await SendAndReceiveAsync(destination, requestMessage);
        if (replyMessage != null)
        {
            return DoConvert<T>(replyMessage);
        }

        return default(T);
    }

    public virtual Task<IMessage> SendAndReceiveAsync(IMessage requestMessage, CancellationToken cancellationToken = default)
    {
        return SendAndReceiveAsync(RequiredDefaultSendDestination, requestMessage, cancellationToken);
    }

    public virtual Task<IMessage> SendAndReceiveAsync(TDestination destination, IMessage requestMessage, CancellationToken cancellationToken = default)
    {
        return DoSendAndReceiveAsync(destination, requestMessage, cancellationToken);
    }

    public virtual T ConvertSendAndReceive<T>(object request)
    {
        return ConvertSendAndReceive<T>(RequiredDefaultSendDestination, request);
    }

    public virtual T ConvertSendAndReceive<T>(TDestination destination, object request)
    {
        return ConvertSendAndReceive<T>(destination, request, (IDictionary<string, object>)null);
    }

    public virtual T ConvertSendAndReceive<T>(TDestination destination, object request, IDictionary<string, object> headers)
    {
        return ConvertSendAndReceive<T>(destination, request, headers, null);
    }

    public virtual T ConvertSendAndReceive<T>(object request, IMessagePostProcessor requestPostProcessor)
    {
        return ConvertSendAndReceive<T>(RequiredDefaultSendDestination, request, requestPostProcessor);
    }

    public virtual T ConvertSendAndReceive<T>(TDestination destination, object request, IMessagePostProcessor requestPostProcessor)
    {
        return ConvertSendAndReceive<T>(destination, request, null, requestPostProcessor);
    }

    public virtual T ConvertSendAndReceive<T>(TDestination destination, object request, IDictionary<string, object> headers, IMessagePostProcessor requestPostProcessor)
    {
        var requestMessage = DoConvert(request, headers, requestPostProcessor);
        var replyMessage = SendAndReceive(destination, requestMessage);
        if (replyMessage != null)
        {
            return DoConvert<T>(replyMessage);
        }

        return default;
    }

    public virtual IMessage SendAndReceive(IMessage requestMessage)
    {
        return SendAndReceive(RequiredDefaultSendDestination, requestMessage);
    }

    public virtual IMessage SendAndReceive(TDestination destination, IMessage requestMessage)
    {
        return DoSendAndReceive(destination, requestMessage);
    }

    protected abstract IMessage DoSendAndReceive(TDestination destination, IMessage requestMessage);

    protected abstract Task<IMessage> DoSendAndReceiveAsync(TDestination destination, IMessage requestMessage, CancellationToken cancellationToken = default);
}
