// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Channel;
using Steeltoe.Messaging;
using Steeltoe.Stream.Config;
using System;
using System.Collections.Generic;

namespace Steeltoe.Stream.Binder
{
    public abstract class AbstractTestBinder<C> : IBinder<IMessageChannel>
        where C : AbstractBinder<IMessageChannel>
    {
        protected HashSet<string> _queues = new ();

        protected HashSet<string> _exchanges = new ();

        public Type TargetType => typeof(IMessageChannel);

        public C CoreBinder { get; private set; }

        public C Binder
        {
            get => CoreBinder;
            set
            {
                try
                {
                    // value.Initialize();
                }
                catch (Exception)
                {
                    // TODO: Log
                    throw;
                }

                CoreBinder = value;
            }
        }

        public IApplicationContext ApplicationContext
        {
            get { return Binder?.ApplicationContext; }
        }

        public string ServiceName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public virtual IBinding BindConsumer(string name, string group, IMessageChannel inboundTarget, IConsumerOptions consumerOptions)
        {
            CheckChannelIsConfigured(inboundTarget, consumerOptions);
            return BindConsumer(name, group, (object)inboundTarget, consumerOptions);
        }

        public virtual IBinding BindConsumer(string name, string group, object inboundTarget, IConsumerOptions consumerOptions)
        {
            _queues.Add(name);
            return CoreBinder.BindConsumer(name, group, inboundTarget, consumerOptions);
        }

        public virtual IBinding BindProducer(string name, IMessageChannel outboundTarget, IProducerOptions producerOptions)
        {
            return BindProducer(name, (object)outboundTarget, producerOptions);
        }

        public IBinding BindProducer(string name, object outboundTarget, IProducerOptions producerOptions)
        {
            _queues.Add(name);
            return CoreBinder.BindProducer(name, outboundTarget, producerOptions);
        }

        public abstract void Cleanup();

        public void Dispose()
        {
            // Nothing to do
        }

        private void CheckChannelIsConfigured(IMessageChannel messageChannel, IConsumerOptions options)
        {
            if (messageChannel is AbstractSubscribableChannel subChan && !options.UseNativeDecoding)
            {
                if (subChan.ChannelInterceptors.Count == 0)
                {
                    throw new InvalidOperationException("'messageChannel' appears to be misconfigured. Consider creating channel via AbstractBinderTest.createBindableChannel(..)");
                }
            }
        }
    }
}
