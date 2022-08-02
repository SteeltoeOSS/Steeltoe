// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;

namespace Steeltoe.Integration.Dispatcher;

public class UnicastingDispatcher : AbstractDispatcher
{
    public UnicastingDispatcher(IApplicationContext context, ILogger logger = null)
        : base(context, null, logger)
    {
    }

    public UnicastingDispatcher(IApplicationContext context, TaskScheduler executor, ILogger logger = null)
        : base(context, executor, logger)
    {
    }

    public override bool Dispatch(IMessage message, CancellationToken cancellationToken = default)
    {
        if (Executor != null)
        {
            Factory.StartNew(() =>
            {
                IMessageHandlingRunnable task = CreateMessageHandlingTask(message, cancellationToken);

                try
                {
                    task.Run();
                }
                catch (Exception e)
                {
                    if (ErrorHandler != null)
                    {
                        ErrorHandler.HandleError(e);
                    }
                }
            }, CancellationToken.None);

            return true;
        }

        return DoDispatch(message, cancellationToken);
    }

    protected override bool DoDispatch(IMessage message, CancellationToken cancellationToken)
    {
        if (TryOptimizedDispatch(message))
        {
            return true;
        }

        List<IMessageHandler> handlers = this.handlers;

        if (handlers.Count == 0)
        {
            throw new MessageDispatchingException(message, "Dispatcher has no subscribers");
        }

        MessageHandlerEnumerator handlerIterator = GetHandlerEnumerator(message, handlers);
        List<Exception> exceptions = null;
        bool isLast;
        bool success = false;

        do
        {
            IMessageHandler handler = handlerIterator.Current;
            isLast = !handlerIterator.MoveNext();

            try
            {
                handler.HandleMessage(message);
                success = true; // we have a winner.
                break;
            }
            catch (Exception e)
            {
                Exception runtimeException = IntegrationUtils.WrapInDeliveryExceptionIfNecessary(message, "Dispatcher failed to deliver Message", e);
                exceptions ??= new List<Exception>();

                exceptions.Add(runtimeException);

                HandleExceptions(exceptions, message, isLast);
            }
        }
        while (!isLast);

        return success;
    }

    private MessageHandlerEnumerator GetHandlerEnumerator(IMessage message, List<IMessageHandler> handlers)
    {
        if (LoadBalancingStrategy != null)
        {
            int index = LoadBalancingStrategy.GetNextHandlerStartIndex(message, handlers);
            return new MessageHandlerEnumerator(index, handlers);
        }

        return new MessageHandlerEnumerator(0, handlers);
    }

    private void HandleExceptions(List<Exception> allExceptions, IMessage message, bool isLast)
    {
        if (isLast || !Failover)
        {
            if (allExceptions != null && allExceptions.Count == 1)
            {
                throw allExceptions[0];
            }

            throw new AggregateMessageDeliveryException(message, "All attempts to deliver Message to MessageHandlers failed.", allExceptions);
        }
    }

    private IMessageHandlingRunnable CreateMessageHandlingTask(IMessage message, CancellationToken cancellationToken)
    {
        var messageHandlingRunnable = new MessageHandlingRunnable(this, message, cancellationToken);

        if (MessageHandlingDecorator != null)
        {
            return MessageHandlingDecorator.Decorate(messageHandlingRunnable);
        }

        return messageHandlingRunnable;
    }

    private sealed class MessageHandlingRunnable : IMessageHandlingRunnable
    {
        public UnicastingDispatcher Dispatcher { get; }

        public IMessage Message { get; }

        public CancellationToken Token { get; }

        public IMessageHandler MessageHandler { get; }

        public MessageHandlingRunnable(UnicastingDispatcher dispatcher, IMessage message, CancellationToken cancellationToken)
        {
            Dispatcher = dispatcher;
            Message = message;
            Token = cancellationToken;
            MessageHandler = new MessageHandlerDelegate(this);
        }

        public bool Run()
        {
            Dispatcher.DoDispatch(Message, Token);
            return true;
        }

        private sealed class MessageHandlerDelegate : IMessageHandler
        {
            private readonly MessageHandlingRunnable _runnable;

            public string ServiceName { get; set; }

            public MessageHandlerDelegate(MessageHandlingRunnable runnable)
            {
                _runnable = runnable;
                ServiceName = $"{GetType().Name}@{GetHashCode()}";
            }

            public void HandleMessage(IMessage message)
            {
                _runnable.Dispatcher.DoDispatch(message, _runnable.Token);
            }
        }
    }

    internal struct MessageHandlerEnumerator
    {
        private readonly int _startIndex;
        private readonly List<IMessageHandler> _handlers;
        private int _index;

        public MessageHandlerEnumerator(int startIndex, List<IMessageHandler> handlers)
        {
            _index = _startIndex = startIndex;
            _handlers = handlers;
        }

        public IMessageHandler Current => _handlers[_index];

        public bool MoveNext()
        {
            _index = (_index + 1) % _handlers.Count;
            return _index != _startIndex;
        }

        public void Reset()
        {
            _index = _startIndex;
        }
    }
}
