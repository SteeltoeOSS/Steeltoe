// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Common.Retry;
using Steeltoe.Integration;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Provisioning;
using System;

namespace Steeltoe.Stream.Binder
{
    public abstract class AbstractPollableMessageSourceBinder : AbstractMessageChannelBinder, IPollableConsumerBinder<IMessageHandler>
    {
        protected AbstractPollableMessageSourceBinder(
            IApplicationContext context,
            string[] headersToEmbed,
            IProvisioningProvider provisioningProvider)
        : this(context, headersToEmbed, provisioningProvider, null, null)
        {
        }

        protected AbstractPollableMessageSourceBinder(
            IApplicationContext context,
            string[] headersToEmbed,
            IProvisioningProvider provisioningProvider,
            IListenerContainerCustomizer containerCustomizer,
            IMessageSourceCustomizer sourceCustomizer)
            : base(context, headersToEmbed, provisioningProvider, containerCustomizer, sourceCustomizer)
        {
        }

        public virtual IBinding BindConsumer(string name, string group, IPollableSource<IMessageHandler> inboundTarget, IConsumerOptions consumerOptions)
        {
            if (!(inboundTarget is DefaultPollableMessageSource bindingTarget))
            {
                throw new InvalidOperationException(nameof(inboundTarget));
            }

            var destination = _provisioningProvider.ProvisionConsumerDestination(name, group, consumerOptions);
            if (consumerOptions.HeaderMode == HeaderMode.EmbeddedHeaders)
            {
                bindingTarget.AddInterceptor(0, _embeddedHeadersChannelInterceptor);
            }

            var resources = CreatePolledConsumerResources(name, group, destination, consumerOptions);

            var messageSource = resources.Source;

            bindingTarget.Source = messageSource;
            if (resources.ErrorInfrastructure != null)
            {
                if (resources.ErrorInfrastructure.ErrorChannel != null)
                {
                    bindingTarget.ErrorChannel = resources.ErrorInfrastructure.ErrorChannel;
                }

                var ems = GetErrorMessageStrategy();
                if (ems != null)
                {
                    bindingTarget.ErrorMessageStrategy = ems;
                }
            }

            if (consumerOptions.MaxAttempts > 1)
            {
                bindingTarget.RetryTemplate = BuildRetryTemplate(consumerOptions);
                bindingTarget.RecoveryCallback = GetPolledConsumerRecoveryCallback(resources.ErrorInfrastructure, consumerOptions);
            }

            PostProcessPollableSource(bindingTarget);
            if (resources.Source is ILifecycle)
            {
                ((ILifecycle)resources.Source).Start();
            }

            var binding = new DefaultPollableChannelBinding(
                            this,
                            name,
                            group,
                            inboundTarget,
                            resources.Source is ILifecycle ? (ILifecycle)resources.Source : null,
                            consumerOptions,
                            destination);
            return binding;
        }

        public virtual IBinding BindProducer(string name, IPollableSource<IMessageHandler> outboundTarget, IProducerOptions producerOptions)
       {
                throw new NotImplementedException();
        }

        protected virtual void PostProcessPollableSource(DefaultPollableMessageSource bindingTarget)
        {
        }

        protected virtual IRecoveryCallback GetPolledConsumerRecoveryCallback(ErrorInfrastructure errorInfrastructure, IConsumerOptions options)
        {
            return errorInfrastructure.Recoverer;
        }

        protected virtual PolledConsumerResources CreatePolledConsumerResources(string name, string group, IConsumerDestination destination, IConsumerOptions consumerOptions)
        {
            throw new InvalidOperationException("This binder does not support pollable consumers");
        }
    }
}
