﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
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
            IProvisioningProvider provisioningProvider,
            ILogger logger)
        : this(context, headersToEmbed, provisioningProvider, null, null, logger)
        {
        }

        protected AbstractPollableMessageSourceBinder(
            IApplicationContext context,
            string[] headersToEmbed,
            IProvisioningProvider provisioningProvider,
            IListenerContainerCustomizer containerCustomizer,
            IMessageSourceCustomizer sourceCustomizer,
            ILogger logger)
            : base(context, headersToEmbed, provisioningProvider, containerCustomizer, sourceCustomizer, logger)
        {
        }

        public override IBinding BindConsumer(string name, string group, object inboundTarget, IConsumerOptions consumerOptions)
        {
            if (inboundTarget is IPollableSource<IMessageHandler>)
            {
                return BindConsumer(name, group, (IPollableSource<IMessageHandler>)inboundTarget, consumerOptions);
            }

            return base.BindConsumer(name, group, inboundTarget, consumerOptions);
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
            var sourceAsLifecycle = resources.Source as ILifecycle;
            if (sourceAsLifecycle != null)
            {
                sourceAsLifecycle.Start();
            }

            var binding = new DefaultPollableChannelBinding(
                            this,
                            name,
                            group,
                            inboundTarget,
                            sourceAsLifecycle != null ? sourceAsLifecycle : null,
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
