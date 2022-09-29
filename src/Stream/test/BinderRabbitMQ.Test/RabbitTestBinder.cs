// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging;
using Steeltoe.Messaging.RabbitMQ.Configuration;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Stream.Binder.Rabbit.Provisioning;
using Steeltoe.Stream.Binder.RabbitMQ.Configuration;
using Steeltoe.Stream.Configuration;

namespace Steeltoe.Stream.Binder.Rabbit;

public class RabbitTestBinder : AbstractPollableConsumerTestBinder<RabbitMessageChannelBinder>
{
    private static IApplicationContext _applicationContext;
    private readonly RabbitAdmin _rabbitAdmin;
    private readonly HashSet<string> _prefixes = new();

    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;

    public RabbitBindingsOptions BindingsOptions { get; }

    public RabbitTestBinder(IConnectionFactory connectionFactory, IOptionsMonitor<RabbitOptions> rabbitOptions,
        IOptionsMonitor<RabbitBinderOptions> binderOptions, IOptionsMonitor<RabbitBindingsOptions> bindingsOptions, ILoggerFactory loggerFactory)
        : this(connectionFactory,
            new RabbitMessageChannelBinder(GetApplicationContext(), loggerFactory.CreateLogger<RabbitMessageChannelBinder>(), connectionFactory, rabbitOptions,
                binderOptions, bindingsOptions,
                new RabbitExchangeQueueProvisioner(connectionFactory, bindingsOptions, GetApplicationContext(),
                    loggerFactory.CreateLogger<RabbitExchangeQueueProvisioner>())), loggerFactory.CreateLogger<RabbitTestBinder>())
    {
        BindingsOptions = bindingsOptions.CurrentValue;
        _loggerFactory = loggerFactory;
    }

    public RabbitTestBinder(IConnectionFactory connectionFactory, RabbitMessageChannelBinder binder, ILogger<RabbitTestBinder> logger)
    {
        _logger = logger;
        PollableConsumerBinder = binder;
        _rabbitAdmin = new RabbitAdmin(GetApplicationContext(), connectionFactory, logger);
        BindingsOptions = binder.BindingsOptions;
    }

    public static IApplicationContext GetApplicationContext()
    {
        if (_applicationContext == null)
        {
            ServiceProvider serviceProvider = new ServiceCollection().BuildServiceProvider();
            _applicationContext = new GenericApplicationContext(serviceProvider, new ConfigurationBuilder().Build());
        }

        return _applicationContext;
    }

    public void ResetApplicationContext()
    {
        _applicationContext = null;
        GetApplicationContext();
    }

    public override IBinding BindConsumer(string name, string group, IMessageChannel inboundTarget, IConsumerOptions consumerOptions)
    {
        CaptureConsumerResources(name, group, consumerOptions);

        return base.BindConsumer(name, group, inboundTarget, consumerOptions);
    }

    public IBinding BindPollableConsumer(string name, string group, IPollableSource<IMessageHandler> inboundBindTarget, IConsumerOptions consumerOptions)
    {
        CaptureConsumerResources(name, group, consumerOptions);
        return BindConsumer(name, group, inboundBindTarget, consumerOptions);
    }

    public override IBinding BindProducer(string name, IMessageChannel outboundTarget, IProducerOptions producerOptions)
    {
        RabbitProducerOptions properties = BindingsOptions.GetRabbitProducerOptions(producerOptions.BindingName);

        Queues.Add($"{properties.Prefix}{name}.default");
        Exchanges.Add(properties.Prefix + name);

        if (producerOptions.RequiredGroups != null)
        {
            foreach (string group in producerOptions.RequiredGroups)
            {
                if (properties.QueueNameGroupOnly == true)
                {
                    Queues.Add(properties.Prefix + group);
                }
                else
                {
                    Queues.Add($"{properties.Prefix}{name}.{group}");
                }
            }
        }

        _prefixes.Add(properties.Prefix);
        DeadLetters(properties);

        return base.BindProducer(name, outboundTarget, producerOptions);
    }

    public void ResetConnectionFactoryTimeout()
    {
        string host = _rabbitAdmin.ConnectionFactory.Host;
        int port = _rabbitAdmin.ConnectionFactory.Port;

        _rabbitAdmin.ConnectionFactory = _rabbitAdmin.RabbitTemplate.ConnectionFactory = new CachingConnectionFactory(host, port, _loggerFactory);
    }

    public override void Cleanup()
    {
        foreach (string name in Queues)
        {
            _logger.LogInformation("Deleting queue {queue}", name);
            _rabbitAdmin.DeleteQueue(name);
            _rabbitAdmin.DeleteQueue($"{name}.dlq");

            // delete any partitioned queues
            for (int i = 0; i < 10; i++)
            {
                _rabbitAdmin.DeleteQueue($"{name}-{i}");
                _rabbitAdmin.DeleteQueue($"{name}-{i}.dlq");
            }
        }

        foreach (string exchange in Exchanges)
        {
            _logger.LogInformation("Deleting exchange {exchange}", exchange);
            _rabbitAdmin.DeleteExchange(exchange);
        }

        foreach (string prefix in _prefixes)
        {
            _rabbitAdmin.DeleteExchange($"{prefix}DLX");
        }

        _applicationContext = null;
    }

    private void CaptureConsumerResources(string name, string group, IConsumerOptions options)
    {
        string[] names = null;
        RabbitConsumerOptions consumerOptions = BindingsOptions.GetRabbitConsumerOptions(options.BindingName);

        if (group != null)
        {
            if (consumerOptions.QueueNameGroupOnly.GetValueOrDefault())
            {
                Queues.Add(consumerOptions.Prefix + group);
            }
            else
            {
                if (options.Multiplex)
                {
                    names = name.Split(',');

                    foreach (string nextName in names)
                    {
                        Queues.Add($"{consumerOptions.Prefix}{nextName.Trim()}.{group}");
                    }
                }
                else
                {
                    Queues.Add($"{consumerOptions.Prefix}{name}.{group}");
                }
            }
        }

        if (names != null)
        {
            foreach (string nextName in names)
            {
                Exchanges.Add(consumerOptions.Prefix + nextName.Trim());
            }
        }
        else
        {
            Exchanges.Add(consumerOptions.Prefix + name.Trim());
        }

        _prefixes.Add(consumerOptions.Prefix);
        DeadLetters(consumerOptions);
    }

    private void DeadLetters(RabbitCommonOptions properties)
    {
        if (properties.DeadLetterExchange != null)
        {
            Exchanges.Add(properties.DeadLetterExchange);
        }

        if (properties.DeadLetterQueueName != null)
        {
            Queues.Add(properties.DeadLetterQueueName);
        }
    }
}
