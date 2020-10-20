using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        //public IApplicationContext ApplicationContext
        //{
        //    get { base.PollableConsumerBinder.ap}
        //}

        public static IApplicationContext GetApplicationContext()
        {
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            return new GenericApplicationContext(serviceProvider, new ConfigurationBuilder().Build());
        }

        public RabbitTestBinder(IConnectionFactory connectionFactory, RabbitOptions rabbitOptions, RabbitBinderOptions binderOptions, RabbitBindingsOptions bindingsOptions)
            : this(connectionFactory, new RabbitMessageChannelBinder(GetApplicationContext(), connectionFactory, rabbitOptions, binderOptions, bindingsOptions, new RabbitExchangeQueueProvisioner(connectionFactory, bindingsOptions)))
        {
        }

        public RabbitTestBinder(IConnectionFactory connectionFactory, RabbitMessageChannelBinder binder)
        {
            //var serviceCollection = new ServiceCollection();
            //var serviceProvider = serviceCollection.BuildServiceProvider();
            //var applicationContext = new GenericApplicationContext(serviceProvider, new ConfigurationBuilder().Build());
            //binder.setApplicationContext do we need this?
            
            PollableConsumerBinder = binder;
            _rabbitAdmin = new RabbitAdmin(connectionFactory);

        }

        public IBinding BindConsumer(string name, string group, IMessageChannel moduleInputChannel, ExtendedConsumerOptions<RabbitConsumerOptions> consumerOptions)
        {
            CaptureConsumerResources(name, group, consumerOptions);

            return base.BindConsumer(name, group, moduleInputChannel, consumerOptions);
        }

        public IBinding BindPollableConsumer(string name, string group, IPollableSource<IMessageHandler> inboundBindTarget, ExtendedConsumerOptions<RabbitConsumerOptions> consumerOptions)
        {
            CaptureConsumerResources(name, group, consumerOptions);
            return BindConsumer(name, group, inboundBindTarget, consumerOptions);
        }

        public override void Cleanup()
        {
            foreach (var q in _queues)
            {
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
                _rabbitAdmin.DeleteExchange(exchange);
            }

            foreach (var prefix in _prefixes)
            {
                _rabbitAdmin.DeleteExchange(prefix + "DLX");
            }

            //this.applicationContext.close();
        }

        private void CaptureConsumerResources(string name, string group, ExtendedConsumerOptions<RabbitConsumerOptions> consumerOptions)
        {
            string [] names = null;
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
