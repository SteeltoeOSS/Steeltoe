using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Stream.Binder.Rabbit.Provisioning;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Stream.Binder.Rabbit
{
    public class RabbitTestBinder : AbstractPollableConsumerTestBinder<RabbitMessageChannelBinder>
    {
        public RabbitTestBinder(IConnectionFactory connectionFactory, RabbitOptions rabbitProperties)
            : this(connectionFactory, new RabbitMessageChannelBinder(connectionFactory, rabbitProperties, new RabbitExchangeQueueProvisioner(connectionFactory)))
        {
        }

        public RabbitTestBinder(IConnectionFactory connectionFactory, RabbitMessageChannelBinder binder)
        {
            this.applicationContext = new AnnotationConfigApplicationContext(Config);
            binder.setApplicationContext(this.applicationContext);
            this.setPollableConsumerBinder(binder);
            this.rabbitAdmin = new RabbitAdmin(connectionFactory);
        }
    }
}
