// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Expression.Internal.Contexts;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ.Batch;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Listener.Adapters;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

public abstract class AbstractRabbitListenerEndpoint : IRabbitListenerEndpoint
{
    protected readonly ILogger Logger;
    protected readonly ILoggerFactory LoggerFactory;
    private IApplicationContext _applicationContext;

    protected IServiceExpressionResolver Resolver { get; set; }

    protected IServiceResolver ServiceResolver { get; set; }

    protected IServiceExpressionContext ExpressionContext { get; set; }

    public IApplicationContext ApplicationContext
    {
        get => _applicationContext;
        set
        {
            _applicationContext = value;

            if (_applicationContext != null)
            {
                Resolver = _applicationContext.ServiceExpressionResolver;
                ExpressionContext = new ServiceExpressionContext(_applicationContext);
                ServiceResolver = new ServiceFactoryResolver(_applicationContext);
            }
        }
    }

    public string Id { get; set; }

    public List<IQueue> Queues { get; } = new();

    public List<string> QueueNames { get; } = new();

    public bool Exclusive { get; set; }

    public int? Priority { get; set; }

    public int? Concurrency { get; set; }

    public IRabbitAdmin Admin { get; set; }

    public bool? AutoStartup { get; set; }

    public ISmartMessageConverter MessageConverter { get; set; }

    public bool BatchListener { get; set; }

    public IBatchingStrategy BatchingStrategy { get; set; }

    public AcknowledgeMode? AckMode { get; set; }

    public IReplyPostProcessor ReplyPostProcessor { get; set; }

    public string Group { get; set; }

    protected AbstractRabbitListenerEndpoint(IApplicationContext applicationContext, ILoggerFactory loggerFactory = null)
    {
        ApplicationContext = applicationContext;
        LoggerFactory = loggerFactory;
        Logger = loggerFactory?.CreateLogger(GetType());

        if (applicationContext != null)
        {
            Resolver = applicationContext.ServiceExpressionResolver;
            ExpressionContext = new ServiceExpressionContext(applicationContext);
            ServiceResolver = new ServiceFactoryResolver(applicationContext);
        }
    }

    public void SetQueues(params IQueue[] queues)
    {
        ArgumentGuard.NotNull(queues);

        Queues.Clear();
        Queues.AddRange(queues);
    }

    public void SetQueueNames(params string[] queueNames)
    {
        ArgumentGuard.NotNull(queueNames);

        QueueNames.Clear();
        QueueNames.AddRange(queueNames);
    }

    public void SetupListenerContainer(IMessageListenerContainer listenerContainer)
    {
        var container = (AbstractMessageListenerContainer)listenerContainer;

        bool queuesEmpty = Queues.Count == 0;
        bool queueNamesEmpty = QueueNames.Count == 0;

        if (!queuesEmpty && !queueNamesEmpty)
        {
            throw new InvalidOperationException($"Queues or queue names must be provided but not both for {this}");
        }

        if (queuesEmpty)
        {
            List<string> names = QueueNames;
            container.SetQueueNames(names.ToArray());
        }
        else
        {
            List<IQueue> instances = Queues;
            container.SetQueues(instances.ToArray());
        }

        container.Exclusive = Exclusive;

        if (Priority.HasValue)
        {
            var args = new Dictionary<string, object>
            {
                { "x-priority", Priority.Value }
            };

            container.ConsumerArguments = args;
        }

        if (Admin != null)
        {
            container.RabbitAdmin = Admin;
        }

        SetupMessageListener(listenerContainer);
    }

    public override string ToString()
    {
        return GetEndpointDescription().ToString();
    }

    protected abstract IMessageListener CreateMessageListener(IMessageListenerContainer container);

    protected virtual StringBuilder GetEndpointDescription()
    {
        var result = new StringBuilder();

        return result.Append(GetType().Name).Append('[').Append(Id).Append("] queues=").Append(Queues).Append("' | queueNames='").Append(QueueNames)
            .Append("' | exclusive='").Append(Exclusive).Append("' | priority='").Append(Priority).Append("' | admin='").Append(Admin).Append('\'');
    }

    private void SetupMessageListener(IMessageListenerContainer container)
    {
        IMessageListener messageListener = CreateMessageListener(container);

        if (messageListener == null)
        {
            throw new InvalidOperationException($"Endpoint [{this}] must provide a non null message listener");
        }

        container.SetupMessageListener(messageListener);
    }
}
