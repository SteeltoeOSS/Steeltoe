using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        private readonly RabbitAdmin _rabbitAdmin;

        private readonly HashSet<string> _prefixes = new HashSet<string>();

        private static IApplicationContext _applicationContext;

        private readonly ILogger _logger;

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

        public RabbitTestBinder(IConnectionFactory connectionFactory, RabbitOptions rabbitOptions, RabbitBinderOptions binderOptions, RabbitBindingsOptions bindingsOptions, ILogger logger)
            : this(connectionFactory, new RabbitMessageChannelBinder(GetApplicationContext(), logger, connectionFactory, rabbitOptions, binderOptions, bindingsOptions, new RabbitExchangeQueueProvisioner(connectionFactory, bindingsOptions, GetApplicationContext(), logger)), logger)
        {
        }

        public RabbitTestBinder(IConnectionFactory connectionFactory, RabbitMessageChannelBinder binder, ILogger logger)
        {
            //var serviceCollection = new ServiceCollection();
            //var serviceProvider = serviceCollection.BuildServiceProvider();
            //var applicationContext = new GenericApplicationContext(serviceProvider, new ConfigurationBuilder().Build());
            //binder.setApplicationContext do we need this?
            _logger = logger;
            PollableConsumerBinder = binder;
            _rabbitAdmin = new RabbitAdmin(connectionFactory, logger);

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
            var properties = producerOptions as ExtendedProducerOptions<RabbitProducerOptions>;
            _queues.Add(properties.Extension.Prefix + name + ".default");
            _exchanges.Add(properties.Extension.Prefix + name);

            if (properties.RequiredGroups != null)
            {
                foreach (var group in properties.RequiredGroups)
                {
                    if (properties.Extension.QueueNameGroupOnly == true)
                    {
                        _queues.Add(properties.Extension.Prefix + group);
                    }
                    else
                    {
                        _queues.Add(properties.Extension.Prefix + name + "." + group);
                    }
                }
            }

            _prefixes.Add(properties.Extension.Prefix);
            DeadLetters(properties.Extension);

            return base.BindProducer(name, outboundTarget, properties);
        }

        public override void Cleanup()
        {
            foreach (var q in _queues)
            {
                _logger.LogInformation("Deleting queue " + q);
                _rabbitAdmin.DeleteQueue(q);
                _rabbitAdmin.DeleteQueue(q + ".dlq");

                // delete any partitioned queues
                for (int i = 0; i < 10; i++)
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
            string [] names = null;
            var consumerOptions = options as ExtendedConsumerOptions<RabbitConsumerOptions>;
            if (group != null)
            {
                if (consumerOptions.Extension.QueueNameGroupOnly.GetValueOrDefault())
                {
                    _queues.Add(consumerOptions.Extension.Prefix + group);
                }
                else
                {
                    if (consumerOptions.Multiplex.GetValueOrDefault())
                    {
                        names = name.Split(',');
                        foreach (var nayme in names)
                        {
                            _queues.Add(consumerOptions.Extension.Prefix + nayme.Trim() + "." + group);
                        }
                    }
                    else
                    {
                        _queues.Add(consumerOptions.Extension.Prefix + name + "." + group);
                    }
                }
            }

            if (names != null)
            {
                foreach (var nayme in names)
                {
                    _exchanges.Add(consumerOptions.Extension.Prefix + nayme.Trim());
                }
            }
            else
            {
                _exchanges.Add(consumerOptions.Extension.Prefix + name.Trim());
            }

            _prefixes.Add(consumerOptions.Extension.Prefix);
            DeadLetters(consumerOptions.Extension);
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
