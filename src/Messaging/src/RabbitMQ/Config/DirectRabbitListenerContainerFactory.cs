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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Listener;

namespace Steeltoe.Messaging.Rabbit.Config
{
    public class DirectRabbitListenerContainerFactory : AbstractRabbitListenerContainerFactory<DirectMessageListenerContainer>
    {
        public const string DEFAULT_SERVICE_NAME = "rabbitListenerContainerFactory";

        public DirectRabbitListenerContainerFactory(IApplicationContext applicationContext, ILoggerFactory loggerFactory = null)
        : base(applicationContext, loggerFactory)
        {
        }

        public DirectRabbitListenerContainerFactory(IApplicationContext applicationContext, IConnectionFactory connectionFactory, ILoggerFactory loggerFactory = null)
            : base(applicationContext, connectionFactory, loggerFactory)
        {
        }

        [ActivatorUtilitiesConstructor]
        public DirectRabbitListenerContainerFactory(IApplicationContext applicationContext, IOptionsMonitor<RabbitOptions> optionsMonitor, IConnectionFactory connectionFactory, ILoggerFactory loggerFactory = null)
            : base(applicationContext, optionsMonitor, connectionFactory, loggerFactory)
        {
            Configure(Options);
        }

        public int? MonitorInterval { get; set; }

        public int? ConsumersPerQueue { get; set; } = 1;

        public int? MessagesPerAck { get; set; }

        public int? AckTimeout { get; set; }

        protected override DirectMessageListenerContainer CreateContainerInstance()
        {
            return new DirectMessageListenerContainer(ApplicationContext, ConnectionFactory, null, _loggerFactory);
        }

        protected override void InitializeContainer(DirectMessageListenerContainer instance, IRabbitListenerEndpoint endpoint)
        {
            base.InitializeContainer(instance, endpoint);
            if (MonitorInterval.HasValue)
            {
                instance.MonitorInterval = MonitorInterval.Value;
            }

            if (MessagesPerAck.HasValue)
            {
                instance.MessagesPerAck = MessagesPerAck.Value;
            }

            if (AckTimeout.HasValue)
            {
                instance.AckTimeout = AckTimeout.Value;
            }

            if (endpoint != null && endpoint.Concurrency.HasValue)
            {
                instance.ConsumersPerQueue = endpoint.Concurrency.Value;
            }
            else if (ConsumersPerQueue.HasValue)
            {
                instance.ConsumersPerQueue = ConsumersPerQueue.Value;
            }
        }

        private void Configure(RabbitOptions options)
        {
            var containerOptions = options.Listener.Direct;
            AutoStartup = containerOptions.AutoStartup;
            PossibleAuthenticationFailureFatal = containerOptions.PossibleAuthenticationFailureFatal;
            if (containerOptions.AcknowledgeMode.HasValue)
            {
                AcknowledgeMode = containerOptions.AcknowledgeMode.Value;
            }

            if (containerOptions.Prefetch.HasValue)
            {
                PrefetchCount = containerOptions.Prefetch.Value;
            }

            DefaultRequeueRejected = containerOptions.DefaultRequeueRejected;
            if (containerOptions.IdleEventInterval.HasValue)
            {
                var asMilli = (int)containerOptions.IdleEventInterval.Value.TotalMilliseconds;
                IdleEventInterval = asMilli;
            }

            MissingQueuesFatal = containerOptions.MissingQueuesFatal;
            var retry = containerOptions.Retry;
            if (retry.Enabled)
            {
                // RetryInterceptorBuilder <?, ?> builder = (retryConfig.isStateless())
                //         ? RetryInterceptorBuilder.stateless()
                //         : RetryInterceptorBuilder.stateful();
                // RetryTemplate retryTemplate = new RetryTemplateFactory(
                //        this.retryTemplateCustomizers).createRetryTemplate(retryConfig,
                //                RabbitRetryTemplateCustomizer.Target.LISTENER);
                // builder.retryOperations(retryTemplate);
                // MessageRecoverer recoverer = (this.messageRecoverer != null)
                //        ? this.messageRecoverer : new RejectAndDontRequeueRecoverer();
                // builder.recoverer(recoverer);
                // factory.setAdviceChain(builder.build());
            }

            if (containerOptions.ConsumersPerQueue.HasValue)
            {
                ConsumersPerQueue = containerOptions.ConsumersPerQueue.Value;
            }
        }
    }
}
