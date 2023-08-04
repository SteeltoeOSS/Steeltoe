// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Listener;

namespace Steeltoe.Messaging.RabbitMQ.Configuration;

public class DirectRabbitListenerContainerFactory : AbstractRabbitListenerContainerFactory<DirectMessageListenerContainer>
{
    public const string DefaultServiceName = "rabbitListenerContainerFactory";

    public int? MonitorInterval { get; set; }

    public int? ConsumersPerQueue { get; set; } = 1;

    public int? MessagesPerAck { get; set; }

    public int? AckTimeout { get; set; }

#pragma warning disable RS0016 // Add public types and members to the declared API
    public DirectRabbitListenerContainerFactory(IApplicationContext applicationContext, ILoggerFactory loggerFactory)
#pragma warning restore RS0016 // Add public types and members to the declared API
        : base(applicationContext, loggerFactory)
    {
    }

#pragma warning disable RS0016 // Add public types and members to the declared API
    public DirectRabbitListenerContainerFactory(IApplicationContext applicationContext, IConnectionFactory connectionFactory,
#pragma warning restore RS0016 // Add public types and members to the declared API
        ILoggerFactory loggerFactory)
        : base(applicationContext, connectionFactory, loggerFactory)
    {
    }

    [ActivatorUtilitiesConstructor]
#pragma warning disable RS0016 // Add public types and members to the declared API
    public DirectRabbitListenerContainerFactory(IApplicationContext applicationContext, IOptionsMonitor<RabbitOptions> optionsMonitor,
#pragma warning restore RS0016 // Add public types and members to the declared API
        IConnectionFactory connectionFactory, ILoggerFactory loggerFactory)
        : base(applicationContext, optionsMonitor, connectionFactory, loggerFactory)
    {
        Configure(Options);
    }

    protected override DirectMessageListenerContainer CreateContainerInstance()
    {
        return new DirectMessageListenerContainer(ApplicationContext, ConnectionFactory, null, LoggerFactory);
    }

    protected override void InitializeContainer(DirectMessageListenerContainer instance, IRabbitListenerEndpoint endpoint)
    {
        base.InitializeContainer(instance, endpoint);

        if (MonitorInterval.HasValue)
        {
            instance.MonitorInterval = MonitorInterval.Value;
        }

        if (MessagesPerAck.HasValue)
        {
            instance.MessagesPerAck = MessagesPerAck.Value;
        }

        if (AckTimeout.HasValue)
        {
            instance.AckTimeout = AckTimeout.Value;
        }

        if (endpoint != null && endpoint.Concurrency.HasValue)
        {
            instance.ConsumersPerQueue = endpoint.Concurrency.Value;
        }
        else if (ConsumersPerQueue.HasValue)
        {
            instance.ConsumersPerQueue = ConsumersPerQueue.Value;
        }
    }

    private void Configure(RabbitOptions options)
    {
        RabbitOptions.DirectContainerOptions containerOptions = options.Listener.Direct;
        AutoStartup = containerOptions.AutoStartup;
        PossibleAuthenticationFailureFatal = containerOptions.PossibleAuthenticationFailureFatal;

        if (containerOptions.AcknowledgeMode.HasValue)
        {
            AcknowledgeMode = containerOptions.AcknowledgeMode.Value;
        }

        if (containerOptions.Prefetch.HasValue)
        {
            PrefetchCount = containerOptions.Prefetch.Value;
        }

        DefaultRequeueRejected = containerOptions.DefaultRequeueRejected;

        if (containerOptions.IdleEventInterval.HasValue)
        {
            int asMilliseconds = (int)containerOptions.IdleEventInterval.Value.TotalMilliseconds;
            IdleEventInterval = asMilliseconds;
        }

        MissingQueuesFatal = containerOptions.MissingQueuesFatal;

        if (containerOptions.ConsumersPerQueue.HasValue)
        {
            ConsumersPerQueue = containerOptions.ConsumersPerQueue.Value;
        }
    }
}
