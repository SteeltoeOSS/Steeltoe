// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;

namespace Steeltoe.Messaging.Core;

public abstract class AbstractDestinationResolvingMessagingTemplate<TDestination>
    : AbstractMessagingTemplate<TDestination>, IDestinationResolvingMessageSendingOperations<TDestination>,
        IDestinationResolvingMessageReceivingOperations<TDestination>, IDestinationResolvingMessageRequestReplyOperations<TDestination>
{
    private IDestinationResolver<TDestination> _destinationResolver;

    public virtual IApplicationContext ApplicationContext { get; }

    public IDestinationResolver<TDestination> DestinationResolver
    {
        get
        {
            _destinationResolver ??= (IDestinationResolver<TDestination>)ApplicationContext?.GetService(typeof(IDestinationResolver<TDestination>));
            return _destinationResolver;
        }

        set => _destinationResolver = value;
    }

    protected AbstractDestinationResolvingMessagingTemplate(IApplicationContext context)
    {
        ApplicationContext = context;
    }

    public virtual Task ConvertAndSendAsync(string destinationName, object payload, CancellationToken cancellationToken = default)
    {
        return ConvertAndSendAsync(destinationName, payload, null, null, cancellationToken);
    }

    public virtual Task ConvertAndSendAsync(string destinationName, object payload, IDictionary<string, object> headers,
        CancellationToken cancellationToken = default)
    {
        return ConvertAndSendAsync(destinationName, payload, headers, null, cancellationToken);
    }

    public virtual Task ConvertAndSendAsync(string destinationName, object payload, IMessagePostProcessor postProcessor,
        CancellationToken cancellationToken = default)
    {
        return ConvertAndSendAsync(destinationName, payload, null, postProcessor, cancellationToken);
    }

    public virtual Task ConvertAndSendAsync(string destinationName, object payload, IDictionary<string, object> headers, IMessagePostProcessor postProcessor,
        CancellationToken cancellationToken = default)
    {
        TDestination destination = ResolveDestination(destinationName);
        return ConvertAndSendAsync(destination, payload, headers, postProcessor, cancellationToken);
    }

    public virtual Task<T> ConvertSendAndReceiveAsync<T>(string destinationName, object request, CancellationToken cancellationToken = default)
    {
        TDestination destination = ResolveDestination(destinationName);
        return ConvertSendAndReceiveAsync<T>(destination, request, cancellationToken);
    }

    public virtual Task<T> ConvertSendAndReceiveAsync<T>(string destinationName, object request, IDictionary<string, object> headers,
        CancellationToken cancellationToken = default)
    {
        TDestination destination = ResolveDestination(destinationName);
        return ConvertSendAndReceiveAsync<T>(destination, request, headers, cancellationToken);
    }

    public virtual Task<T> ConvertSendAndReceiveAsync<T>(string destinationName, object request, IMessagePostProcessor requestPostProcessor,
        CancellationToken cancellationToken = default)
    {
        TDestination destination = ResolveDestination(destinationName);
        return ConvertSendAndReceiveAsync<T>(destination, request, requestPostProcessor, cancellationToken);
    }

    public virtual Task<T> ConvertSendAndReceiveAsync<T>(string destinationName, object request, IDictionary<string, object> headers,
        IMessagePostProcessor requestPostProcessor, CancellationToken cancellationToken = default)
    {
        TDestination destination = ResolveDestination(destinationName);
        return ConvertSendAndReceiveAsync<T>(destination, request, headers, requestPostProcessor, cancellationToken);
    }

    public virtual Task<IMessage> ReceiveAsync(string destinationName, CancellationToken cancellationToken = default)
    {
        TDestination destination = ResolveDestination(destinationName);
        return ReceiveAsync(destination, cancellationToken);
    }

    public virtual Task<T> ReceiveAndConvertAsync<T>(string destinationName, CancellationToken cancellationToken = default)
    {
        TDestination destination = ResolveDestination(destinationName);
        return ReceiveAndConvertAsync<T>(destination, cancellationToken);
    }

    public virtual Task SendAsync(string destinationName, IMessage message, CancellationToken cancellationToken = default)
    {
        TDestination destination = ResolveDestination(destinationName);
        return SendAsync(destination, message, cancellationToken);
    }

    public virtual Task<IMessage> SendAndReceiveAsync(string destinationName, IMessage requestMessage, CancellationToken cancellationToken = default)
    {
        TDestination destination = ResolveDestination(destinationName);
        return SendAndReceiveAsync(destination, requestMessage, cancellationToken);
    }

    public virtual void ConvertAndSend(string destinationName, object payload)
    {
        ConvertAndSend(destinationName, payload, null, null);
    }

    public virtual void ConvertAndSend(string destinationName, object payload, IDictionary<string, object> headers)
    {
        ConvertAndSend(destinationName, payload, headers, null);
    }

    public virtual void ConvertAndSend(string destinationName, object payload, IMessagePostProcessor postProcessor)
    {
        ConvertAndSend(destinationName, payload, null, postProcessor);
    }

    public virtual void ConvertAndSend(string destinationName, object payload, IDictionary<string, object> headers, IMessagePostProcessor postProcessor)
    {
        TDestination destination = ResolveDestination(destinationName);
        ConvertAndSend(destination, payload, headers, postProcessor);
    }

    public virtual T ConvertSendAndReceive<T>(string destinationName, object request)
    {
        TDestination destination = ResolveDestination(destinationName);
        return ConvertSendAndReceive<T>(destination, request);
    }

    public virtual T ConvertSendAndReceive<T>(string destinationName, object request, IDictionary<string, object> headers)
    {
        TDestination destination = ResolveDestination(destinationName);
        return ConvertSendAndReceive<T>(destination, request, headers);
    }

    public virtual T ConvertSendAndReceive<T>(string destinationName, object request, IMessagePostProcessor requestPostProcessor)
    {
        TDestination destination = ResolveDestination(destinationName);
        return ConvertSendAndReceive<T>(destination, request, requestPostProcessor);
    }

    public virtual T ConvertSendAndReceive<T>(string destinationName, object request, IDictionary<string, object> headers,
        IMessagePostProcessor requestPostProcessor)
    {
        TDestination destination = ResolveDestination(destinationName);
        return ConvertSendAndReceive<T>(destination, request, headers, requestPostProcessor);
    }

    public virtual IMessage Receive(string destinationName)
    {
        TDestination destination = ResolveDestination(destinationName);
        return Receive(destination);
    }

    public virtual T ReceiveAndConvert<T>(string destinationName)
    {
        TDestination destination = ResolveDestination(destinationName);
        return ReceiveAndConvert<T>(destination);
    }

    public virtual void Send(string destinationName, IMessage message)
    {
        TDestination destination = ResolveDestination(destinationName);
        Send(destination, message);
    }

    public virtual IMessage SendAndReceive(string destinationName, IMessage requestMessage)
    {
        TDestination destination = ResolveDestination(destinationName);
        return SendAndReceive(destination, requestMessage);
    }

    protected TDestination ResolveDestination(string destinationName)
    {
        if (DestinationResolver == null)
        {
            throw new InvalidOperationException("DestinationResolver is required to resolve destination names");
        }

        return DestinationResolver.ResolveDestination(destinationName);
    }
}
