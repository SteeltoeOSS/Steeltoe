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

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.Rabbit.Batch;
using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Expressions;
using Steeltoe.Messaging.Rabbit.Listener.Adapters;
using Steeltoe.Messaging.Rabbit.Support.Converter;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public abstract class AbstractRabbitListenerEndpoint : IRabbitListenerEndpoint
    {
        protected readonly ILogger _logger;

        protected AbstractRabbitListenerEndpoint(IApplicationContext applicationContext, ILogger logger = null)
        {
            ApplicationContext = applicationContext;
            _logger = logger;
        }

        public IApplicationContext ApplicationContext { get; set; }

        public string Id { get; set; }

        public List<Queue> Queues { get; } = new List<Queue>();

        public List<string> QueueNames { get; } = new List<string>();

        public bool Exclusive { get; set; }

        public int? Priority { get; set; }

        public int? Concurrency { get; set; }

        public IAmqpAdmin Admin { get; set; }

        public bool? AutoStartup { get; set; }

        public IMessageConverter MessageConverter { get; set; }

        public bool BatchListener { get; set; }

        public IBatchingStrategy BatchingStrategy { get; set; }

        public AcknowledgeMode? AckMode { get; set; }

        public IReplyPostProcessor ReplyPostProcessor { get; set; }

        public void SetQueues(params Queue[] queues)
        {
            if (queues == null)
            {
                throw new ArgumentNullException(nameof(queues));
            }

            Queues.Clear();
            Queues.AddRange(queues);
        }

        public void SetQueueNames(params string[] queueNames)
        {
            if (queueNames == null)
            {
                throw new ArgumentNullException(nameof(queueNames));
            }

            QueueNames.Clear();
            QueueNames.AddRange(queueNames);
        }

        public void SetupListenerContainer(IMessageListenerContainer listenerContainer)
        {
            var container = (AbstractMessageListenerContainer)listenerContainer;

            var queuesEmpty = Queues.Count == 0;
            var queueNamesEmpty = QueueNames.Count == 0;
            if (!queuesEmpty && !queueNamesEmpty)
            {
                throw new InvalidOperationException("Queues or queue names must be provided but not both for " + this);
            }

            if (queuesEmpty)
            {
                var names = QueueNames;
                container.SetQueueNames(names.ToArray());
            }
            else
            {
                var instances = Queues;
                container.SetQueues(instances.ToArray());
            }

            container.Exclusive = Exclusive;
            if (Priority.HasValue)
            {
                var args = new Dictionary<string, object>();
                args.Add("x-priority", Priority.Value);
                container.ConsumerArguments = args;
            }

            if (Admin != null)
            {
                container.AmqpAdmin = Admin;
            }

            SetupMessageListener(listenerContainer);
        }

        public override string ToString()
        {
            return GetEndpointDescription().ToString();
        }

        protected IServiceExpressionResolver Resolver { get; set; }

        protected IServiceResolver ServiceResolver { get; set; }

        protected IServiceExpressionContext ExpressionContext { get; set; }

        protected abstract IMessageListener CreateMessageListener(IMessageListenerContainer container);

        protected virtual StringBuilder GetEndpointDescription()
        {
            var result = new StringBuilder();
            return result.Append(GetType().Name).Append("[").Append(Id).
                    Append("] queues=").Append(Queues).
                    Append("' | queueNames='").Append(QueueNames).
                    Append("' | exclusive='").Append(Exclusive).
                    Append("' | priority='").Append(Priority).
                    Append("' | admin='").Append(Admin).Append("'");
        }

        private void SetupMessageListener(IMessageListenerContainer container)
        {
            var messageListener = CreateMessageListener(container);
            if (messageListener == null)
            {
                throw new InvalidOperationException("Endpoint [" + this + "] must provide a non null message listener");
            }

            container.SetupMessageListener(messageListener);
        }
    }
}
