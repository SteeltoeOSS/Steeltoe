using Microsoft.Extensions.Configuration;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
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
        public RabbitTestBinder(IConnectionFactory connectionFactory, RabbitOptions rabbitOptions, RabbitBinderOptions binderOptions, RabbitBindingsOptions bindingsOptions)
            : this(connectionFactory, new RabbitMessageChannelBinder(null, connectionFactory, rabbitOptions, binderOptions, bindingsOptions, new RabbitExchangeQueueProvisioner(connectionFactory, bindingsOptions)))
        {
        }

        public RabbitTestBinder(IConnectionFactory connectionFactory, RabbitMessageChannelBinder binder)
        {
        //    this.applicationContext = new AnnotationConfigApplicationContext(Config);
        //    binder.setApplicationContext(this.applicationContext);
        //    this.setPollableConsumerBinder(binder);
        //    this.rabbitAdmin = new RabbitAdmin(connectionFactory);
        }
    }
}
