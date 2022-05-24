// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Channel;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Messaging;

namespace Steeltoe.Stream.Binding
{
    public class SubscribableChannelBindingTargetFactory : AbstractBindingTargetFactory<ISubscribableChannel>
    {
        private readonly IMessageChannelConfigurer _messageChannelConfigurer;

        public SubscribableChannelBindingTargetFactory(IApplicationContext context, CompositeMessageChannelConfigurer messageChannelConfigurer)
            : base(context)
        {
            _messageChannelConfigurer = messageChannelConfigurer;
        }

        public override ISubscribableChannel CreateInput(string name)
        {
            var chan = new DirectWithAttributesChannel(ApplicationContext)
            {
                ServiceName = name
            };
            chan.SetAttribute("type", "input");
            _messageChannelConfigurer.ConfigureInputChannel(chan, name);

            AddChannelInterceptors(chan);

            ApplicationContext.Register(name, chan);

            return chan;
        }

        public override ISubscribableChannel CreateOutput(string name)
        {
            var chan = new DirectWithAttributesChannel(ApplicationContext)
            {
                ServiceName = name
            };
            chan.SetAttribute("type", "output");
            _messageChannelConfigurer.ConfigureOutputChannel(chan, name);

            AddChannelInterceptors(chan);

            ApplicationContext.Register(name, chan);

            return chan;
        }

        private void AddChannelInterceptors(IMessageChannel chan)
        {
            if (chan is IChannelInterceptorAware aware)
            {
                var interceptors = ApplicationContext.GetServices<IChannelInterceptor>();
                foreach (var interceptor in interceptors)
                {
                    aware.AddInterceptor(interceptor);
                }
            }
        }
    }
}
