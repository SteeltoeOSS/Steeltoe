// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using Steeltoe.Stream.Config;
using System;

namespace Steeltoe.Stream.Binder
{
    public class AbstractPollableConsumerTestBinder<C> : AbstractTestBinder<C>, IPollableConsumerBinder<IMessageHandler, IConsumerOptions>
        where C : AbstractBinder<IMessageChannel>
    {
       private IPollableConsumerBinder<IMessageHandler, IConsumerOptions> _binder;

        public IPollableConsumerBinder<IMessageHandler, IConsumerOptions> PollableConsumerBinder
        {
            get => _binder;
            set
            {
                Binder = (C)value;
                _binder = value;
            }
        }

        public IBinding BindPollableConsumer(string name, string group, IPollableSource<IMessageHandler> inboundTarget, IConsumerOptions consumerOptions)
        {
            return _binder.BindPollableConsumer(name, group, inboundTarget, consumerOptions);
        }

        //public IBinding BindProducer(string name, IPollableSource<IMessageHandler> outboundTarget, IProducerOptions producerOptions)
        //{
        //    throw new NotImplementedException();
        //}

        public override void Cleanup()
        {
            throw new NotImplementedException();
        }
    }
}
