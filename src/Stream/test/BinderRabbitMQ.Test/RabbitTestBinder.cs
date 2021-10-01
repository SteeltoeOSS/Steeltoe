// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Stream.Binder.Rabbit.Config;
using Steeltoe.Stream.Binder.Rabbit.Provisioning;
using Steeltoe.Stream.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Stream.Binder.Rabbit
{
    public class RabbitTestBinder : AbstractPollableConsumerTestBinder<RabbitMessageChannelBinder>
    {
        private static IApplicationContext _applicationContext;
        private readonly RabbitAdmin _rabbitAdmin;
        private readonly HashSet<string> _prefixes = new HashSet<string>();

        public RabbitBindingsOptions BindingsOptions { get; }

        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;

        public static IApplicationContext GetApplicationContext()
        {
            if (_applicationContext == null)
            {
                var serviceProvider = new ServiceCollection().BuildServiceProvider();
                _applicationContext = new GenericApplicationContext(serviceProvider, new ConfigurationBuilder().Build());
            }

            return _applicationContext;
        }

        public void ResetApplicationContext()
        {
            _applicationContext = null;
            GetApplicationContext();
        }

        public RabbitTestBinder(IConnectionFactory connectionFactory, IOptionsMonitor<RabbitOptions> rabbitOptions, IOptionsMonitor<RabbitBinderOptions> binderOptions, IOptionsMonitor<RabbitBindingsOptions> bindingsOptions, ILoggerFactory loggerFactory)
            : this(connectionFactory, new RabbitMessageChannelBinder(GetApplicationContext(), loggerFactory.CreateLogger<RabbitMessageChannelBinder>(), connectionFactory, rabbitOptions, binderOptions, bindingsOptions, new RabbitExchangeQueueProvisioner(connectionFactory, bindingsOptions, GetApplicationContext(), loggerFactory.CreateLogger<RabbitExchangeQueueProvisioner>())), loggerFactory.CreateLogger<RabbitTestBinder>())
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

        public override IBinding BindConsumer(string name, string group, IMessageChannel moduleInputChannel, IConsumerOptions consumerOptions)
        {
            CaptureConsumerResources(name, group, consumerOptions);

            return base.BindConsumer(name, group, moduleInputChannel, consumerOptions);
        }

        public IBinding BindPollableConsumer(string name, string group, IPollableSource<IMessageHandler> inboundBindTarget, IConsumerOptions consumerOptions)
        {
            CaptureConsumerResources(name, group, consumerOptions);
            return BindConsumer(name, group, inboundBindTarget, consumerOptions);
        }

        public override IBinding BindProducer(string name, IMessageChannel outboundTarget, IProducerOptions producerOptions)
        {
            var properties = BindingsOptions.GetRabbitProducerOptions(producerOptions.BindingName);

            _queues.Add(properties.Prefix + name + ".default");
            _exchanges.Add(properties.Prefix + name);

            if (producerOptions.RequiredGroups != null)
            {
                foreach (var group in producerOptions.RequiredGroups)
                {
                    if (properties.QueueNameGroupOnly == true)
                    {
                        _queues.Add(properties.Prefix + group);
                    }
                    else
                    {
                        _queues.Add(properties.Prefix + name + "." + group);
                    }
                }
            }

            _prefixes.Add(properties.Prefix);
            DeadLetters(properties);

            return base.BindProducer(name, outboundTarget, producerOptions);
        }

        public void ResetConnectionFactoryTimeout()
        {
            var host = _rabbitAdmin.ConnectionFactory.Host;
            var port = _rabbitAdmin.ConnectionFactory.Port;

            _rabbitAdmin.ConnectionFactory =
            _rabbitAdmin.RabbitTemplate.ConnectionFactory = new CachingConnectionFactory(host, port, _loggerFactory);
        }

        public override void Cleanup()
        {
            foreach (var q in _queues)
            {
                _logger.LogInformation("Deleting queue " + q);
                _rabbitAdmin.DeleteQueue(q);
                _rabbitAdmin.DeleteQueue(q + ".dlq");

                // delete any partitioned queues
                for (var i = 0; i < 10; i++)
                {
                    _rabbitAdmin.DeleteQueue(q + "-" + i);
                    _rabbitAdmin.DeleteQueue(q + "-" + i + ".dlq");
                }
            }

            foreach (var exchange in _exchanges)
            {
                _logger.LogInformation("Deleting exch " + exchange);
                _rabbitAdmin.DeleteExchange(exchange);
            }

            foreach (var prefix in _prefixes)
            {
                _rabbitAdmin.DeleteExchange(prefix + "DLX");
            }

            _applicationContext = null;
        }

        private void CaptureConsumerResources(string name, string group, IConsumerOptions options)
        {
            string[] names = null;
            var consumerOptions = BindingsOptions.GetRabbitConsumerOptions(options.BindingName);
            if (group != null)
            {
                if (consumerOptions.QueueNameGroupOnly.GetValueOrDefault())
                {
                    _queues.Add(consumerOptions.Prefix + group);
                }
                else
                {
                    if (options.Multiplex)
                    {
                        names = name.Split(',');
                        foreach (var nayme in names)
                        {
                            _queues.Add(consumerOptions.Prefix + nayme.Trim() + "." + group);
                        }
                    }
                    else
                    {
                        _queues.Add(consumerOptions.Prefix + name + "." + group);
                    }
                }
            }

            if (names != null)
            {
                foreach (var nayme in names)
                {
                    _exchanges.Add(consumerOptions.Prefix + nayme.Trim());
                }
            }
            else
            {
                _exchanges.Add(consumerOptions.Prefix + name.Trim());
            }

            _prefixes.Add(consumerOptions.Prefix);
            DeadLetters(consumerOptions);
        }

        private void DeadLetters(RabbitCommonOptions properties)
        {
            if (properties.DeadLetterExchange != null)
            {
                _exchanges.Add(properties.DeadLetterExchange);
            }

            if (properties.DeadLetterQueueName != null)
            {
                _queues.Add(properties.DeadLetterQueueName);
            }
        }
    }
}
