// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;

namespace Steeltoe.Integration.Dispatcher;

public class BroadcastingDispatcher : AbstractDispatcher
{
    private readonly bool _requireSubscribers;

    private IMessageBuilderFactory _messageBuilderFactory;

    internal IMessageBuilderFactory MessageBuilderFactory
    {
        get
        {
            _messageBuilderFactory ??= IntegrationServices.MessageBuilderFactory;
            return _messageBuilderFactory;
        }

        set => _messageBuilderFactory = value;
    }

    internal ILogger Logger => InnerLogger;

    public virtual bool IgnoreFailures { get; set; }

    public virtual bool ApplySequence { get; set; }

    public virtual int MinSubscribers { get; set; }

    public BroadcastingDispatcher(IApplicationContext context, ILogger logger = null)
        : this(context, null, false, logger)
    {
    }

    public BroadcastingDispatcher(IApplicationContext context, TaskScheduler executor, ILogger logger = null)
        : this(context, executor, false, logger)
    {
    }

    public BroadcastingDispatcher(IApplicationContext context, bool requireSubscribers, ILogger logger = null)
        : this(context, null, requireSubscribers, logger)
    {
    }

    public BroadcastingDispatcher(IApplicationContext context, TaskScheduler executor, bool requireSubscribers, ILogger logger = null)
        : base(context, executor, logger)
    {
        _requireSubscribers = requireSubscribers;
    }

    protected override bool DoDispatch(IMessage message, CancellationToken cancellationToken)
    {
        int dispatched = 0;
        int sequenceNumber = 1;
        List<IMessageHandler> handlers = this.handlers;

        if (_requireSubscribers && handlers.Count == 0)
        {
            throw new MessageDispatchingException(message, "Dispatcher has no subscribers");
        }

        int sequenceSize = handlers.Count;
        IMessage messageToSend = message;
        string sequenceId = null;

        if (ApplySequence)
        {
            sequenceId = message.Headers.Id;
        }

        foreach (IMessageHandler handler in handlers)
        {
            if (ApplySequence)
            {
                messageToSend = MessageBuilderFactory.FromMessage(message).PushSequenceDetails(sequenceId, sequenceNumber++, sequenceSize).Build();

                if (message is IMessageDecorator decorator)
                {
                    messageToSend = decorator.DecorateMessage(messageToSend);
                }
            }

            if (Executor != null)
            {
                IMessageHandlingRunnable task = CreateMessageHandlingTask(handler, messageToSend);
                Factory.StartNew(() => task.Run(), cancellationToken);
                dispatched++;
            }
            else
            {
                InvokeHandler(handler, messageToSend);
                dispatched++;
            }
        }

        if (dispatched == 0 && MinSubscribers == 0)
        {
            Logger?.LogDebug(sequenceSize > 0 ? "No subscribers received message, default behavior is ignore" : "No subscribers, default behavior is ignore");
        }

        return dispatched >= MinSubscribers;
    }

    protected void InvokeHandler(IMessageHandler handler, IMessage message)
    {
        try
        {
            handler.HandleMessage(message);
        }
        catch (Exception e)
        {
            if (!IgnoreFailures)
            {
                if (Factory != null && ErrorHandler != null)
                {
                    ErrorHandler.HandleError(e);
                    return;
                }

                if (e is MessagingException exception && exception.FailedMessage == null)
                {
                    throw new MessagingException(message, "Failed to handle Message", e);
                }

                throw;
            }

            InnerLogger?.LogWarning("Suppressing Exception since 'ignoreFailures' is set to TRUE.", e);
        }
    }

    private IMessageHandlingRunnable CreateMessageHandlingTask(IMessageHandler handler, IMessage message)
    {
        var messageHandlingRunnable = new MessageHandlingRunnable(this, handler, message);

        if (MessageHandlingDecorator != null)
        {
            return MessageHandlingDecorator.Decorate(messageHandlingRunnable);
        }

        return messageHandlingRunnable;
    }

    private sealed class MessageHandlingRunnable : IMessageHandlingRunnable
    {
        private readonly BroadcastingDispatcher _dispatcher;

        public IMessage Message { get; }

        public IMessageHandler MessageHandler { get; }

        public MessageHandlingRunnable(BroadcastingDispatcher dispatcher, IMessageHandler handler, IMessage message)
        {
            _dispatcher = dispatcher;
            Message = message;
            MessageHandler = handler;
        }

        public bool Run()
        {
            _dispatcher.InvokeHandler(MessageHandler, Message);
            return true;
        }
    }
}
