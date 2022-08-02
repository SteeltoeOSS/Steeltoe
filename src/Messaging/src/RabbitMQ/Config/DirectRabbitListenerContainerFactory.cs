// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Listener;

namespace Steeltoe.Messaging.RabbitMQ.Config;

public class DirectRabbitListenerContainerFactory : AbstractRabbitListenerContainerFactory<DirectMessageListenerContainer>
{
    public const string DefaultServiceName = "rabbitListenerContainerFactory";

    public int? MonitorInterval { get; set; }

    public int? ConsumersPerQueue { get; set; } = 1;

    public int? MessagesPerAck { get; set; }

    public int? AckTimeout { get; set; }

    public DirectRabbitListenerContainerFactory(IApplicationContext applicationContext, ILoggerFactory loggerFactory = null)
        : base(applicationContext, loggerFactory)
    {
    }

    public DirectRabbitListenerContainerFactory(IApplicationContext applicationContext, IConnectionFactory connectionFactory,
        ILoggerFactory loggerFactory = null)
        : base(applicationContext, connectionFactory, loggerFactory)
    {
    }

    [ActivatorUtilitiesConstructor]
    public DirectRabbitListenerContainerFactory(IApplicationContext applicationContext, IOptionsMonitor<RabbitOptions> optionsMonitor,
        IConnectionFactory connectionFactory, ILoggerFactory loggerFactory = null)
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
        RabbitOptions.ListenerRetryOptions retry = containerOptions.Retry;

        if (retry.Enabled)
        {
            // RetryInterceptorBuilder <?, ?> builder = (retryConfig.isStateless())
            //         ? RetryInterceptorBuilder.stateless()
            //         : RetryInterceptorBuilder.stateful();
            // RetryTemplate retryTemplate = new RetryTemplateFactory(
            //        this.retryTemplateCustomizers).createRetryTemplate(retryConfig,
            //                RabbitRetryTemplateCustomizer.Target.LISTENER);
            // builder.retryOperations(retryTemplate);
            // MessageRecoverer recoverer = (this.messageRecoverer != null)
            //        ? this.messageRecoverer : new RejectAndDoNotRequeueRecoverer();
            // builder.recoverer(recoverer);
            // factory.setAdviceChain(builder.build());
        }

        if (containerOptions.ConsumersPerQueue.HasValue)
        {
            ConsumersPerQueue = containerOptions.ConsumersPerQueue.Value;
        }
    }
}
