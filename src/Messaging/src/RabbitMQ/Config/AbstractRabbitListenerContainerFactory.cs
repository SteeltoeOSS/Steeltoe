﻿// Copyright 2017 the original author or authors.
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
using Microsoft.Extensions.Options;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Retry;
using Steeltoe.Common.Transaction;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Rabbit.Batch;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Listener;
using Steeltoe.Messaging.Rabbit.Listener.Adapters;
using Steeltoe.Messaging.Rabbit.Support.Converter;
using Steeltoe.Messaging.Rabbit.Util;
using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Config
{
    public abstract class AbstractRabbitListenerContainerFactory<C> : IRabbitListenerContainerFactory<C>
        where C : AbstractMessageListenerContainer
    {
        protected readonly ILogger _logger;
        private readonly IOptionsMonitor<RabbitOptions> _optionsMonitor;

        protected AbstractRabbitListenerContainerFactory(IApplicationContext applicationContext, ILogger logger = null)
        {
            ApplicationContext = applicationContext;
            _logger = logger;
        }

        protected AbstractRabbitListenerContainerFactory(IApplicationContext applicationContext, IConnectionFactory connectionFactory, ILogger logger = null)
        {
            ApplicationContext = applicationContext;
            _logger = logger;
            ConnectionFactory = connectionFactory;
        }

        protected AbstractRabbitListenerContainerFactory(IApplicationContext applicationContext, IOptionsMonitor<RabbitOptions> optionsMonitor, IConnectionFactory connectionFactory, ILogger logger = null)
        {
            ApplicationContext = applicationContext;
            _logger = logger;
            ConnectionFactory = connectionFactory;
            _optionsMonitor = optionsMonitor;
        }

        protected AbstractRabbitListenerContainerFactory(IApplicationContext applicationContext, IOptionsMonitor<RabbitOptions> optionsMonitor, IConnectionFactory connectionFactory, IMessageConverter messageConverter, ILogger logger = null)
        {
            ApplicationContext = applicationContext;
            _logger = logger;
            ConnectionFactory = connectionFactory;
            MessageConverter = messageConverter;
            _optionsMonitor = optionsMonitor;
        }

        public IApplicationContext ApplicationContext { get; set; }

        public IConnectionFactory ConnectionFactory { get; set; }

        public IErrorHandler ErrorHandler { get; set; }

        public IMessageConverter MessageConverter { get; set; }

        public AcknowledgeMode? AcknowledgeMode { get; set; }

        public bool? ChannelTransacted { get; set; }

        public IPlatformTransactionManager TransactionManager { get; set; }

        public int? PrefetchCount { get; set; }

        public bool? DefaultRequeueRejected { get; set; }

        public int? RecoveryInterval { get; set; }

        public IBackOff RecoveryBackOff { get; set; }

        public bool? MissingQueuesFatal { get; set; }

        public bool? MismatchedQueuesFatal { get; set; }

        public IConsumerTagStrategy ConsumerTagStrategy { get; set; }

        public int? IdleEventInterval { get; set; }

        public int? FailedDeclarationRetryInterval { get; set; }

        public bool? AutoStartup { get; set; }

        public int Phase { get; set; }

        public List<IMessagePostProcessor> AfterReceivePostProcessors { get; set; }

        public List<IMessagePostProcessor> BeforeSendReplyPostProcessors { get; set; }

        public RetryTemplate RetryTemplate { get; set; }

        public IRecoveryCallback ReplyRecoveryCallback { get; set; }

        public Action<C> ContainerCustomizer { get; set; }

        public bool BatchListener { get; set; }

        public IBatchingStrategy BatchingStrategy { get; set; }

        public bool? DeBatchingEnabled { get; set; }

        public virtual string Name { get; set; }

        protected internal RabbitOptions Options
        {
            get
            {
                if (_optionsMonitor != null)
                {
                    return _optionsMonitor.CurrentValue;
                }

                return null;
            }
        }

        public void SetAfterReceivePostProcessors(params IMessagePostProcessor[] postProcessors)
        {
            if (postProcessors == null)
            {
                throw new ArgumentNullException(nameof(postProcessors));
            }

            foreach (var p in postProcessors)
            {
                if (p == null)
                {
                    throw new ArgumentNullException("'postProcessors' cannot have null elements");
                }
            }

            AfterReceivePostProcessors = new List<IMessagePostProcessor>(postProcessors);
        }

        public void SetBeforeSendReplyPostProcessors(params IMessagePostProcessor[] postProcessors)
        {
            if (postProcessors == null)
            {
                throw new ArgumentNullException(nameof(postProcessors));
            }

            foreach (var p in postProcessors)
            {
                if (p == null)
                {
                    throw new ArgumentNullException("'postProcessors' cannot have null elements");
                }
            }

            BeforeSendReplyPostProcessors = new List<IMessagePostProcessor>(postProcessors);
        }

        public C CreateListenerContainer(IRabbitListenerEndpoint endpoint)
        {
            var instance = CreateContainerInstance();

            instance.ConnectionFactory = ConnectionFactory;
            instance.ErrorHandler = ErrorHandler;

            if (MessageConverter != null && endpoint != null)
            {
                endpoint.MessageConverter = MessageConverter;
            }

            if (AcknowledgeMode.HasValue)
            {
                instance.AcknowledgeMode = AcknowledgeMode.Value;
            }

            if (ChannelTransacted.HasValue)
            {
                instance.IsChannelTransacted = ChannelTransacted.Value;
            }

            if (ApplicationContext != null)
            {
                instance.ApplicationContext = ApplicationContext;
            }

            if (TransactionManager != null)
            {
                instance.TransactionManager = TransactionManager;
            }

            if (PrefetchCount.HasValue)
            {
                instance.PrefetchCount = PrefetchCount.Value;
            }

            if (DefaultRequeueRejected.HasValue)
            {
                instance.DefaultRequeueRejected = DefaultRequeueRejected.Value;
            }

            if (RecoveryBackOff != null)
            {
                instance.RecoveryBackOff = RecoveryBackOff;
            }

            if (MismatchedQueuesFatal.HasValue)
            {
                instance.MismatchedQueuesFatal = MismatchedQueuesFatal.Value;
            }

            if (MissingQueuesFatal.HasValue)
            {
                instance.MissingQueuesFatal = MissingQueuesFatal.Value;
            }

            if (ConsumerTagStrategy != null)
            {
                instance.ConsumerTagStrategy = ConsumerTagStrategy;
            }

            if (IdleEventInterval.HasValue)
            {
                instance.IdleEventInterval = IdleEventInterval.Value;
            }

            if (FailedDeclarationRetryInterval.HasValue)
            {
                instance.FailedDeclarationRetryInterval = FailedDeclarationRetryInterval.Value;
            }

            if (AutoStartup.HasValue)
            {
                instance.IsAutoStartup = AutoStartup.Value;
            }

            instance.Phase = Phase;
            if (AfterReceivePostProcessors != null)
            {
                instance.SetAfterReceivePostProcessors(AfterReceivePostProcessors.ToArray());
            }

            if (DeBatchingEnabled.HasValue)
            {
                instance.IsDeBatchingEnabled = DeBatchingEnabled.Value;
            }

            if (BatchListener && DeBatchingEnabled.HasValue)
            {
                // turn off container debatching by default for batch listeners
                instance.IsDeBatchingEnabled = false;
            }

            if (endpoint != null)
            {
                if (endpoint.AutoStartup.HasValue)
                {
                    instance.IsAutoStartup = endpoint.AutoStartup.Value;
                }

                if (endpoint.AckMode.HasValue)
                {
                    instance.AcknowledgeMode = endpoint.AckMode.Value;
                }

                if (endpoint.BatchingStrategy != null)
                {
                    instance.BatchingStrategy = endpoint.BatchingStrategy;
                }

                instance.ListenerId = endpoint.Id;
                endpoint.BatchListener = BatchListener;
                endpoint.SetupListenerContainer(instance);
            }

            var adapterListener = instance.MessageListener as AbstractMessageListenerAdapter;
            if (adapterListener != null)
            {
                if (BeforeSendReplyPostProcessors != null)
                {
                    adapterListener.SetBeforeSendReplyPostProcessors(BeforeSendReplyPostProcessors.ToArray());
                }

                if (RetryTemplate != null)
                {
                    adapterListener.RetryTemplate = RetryTemplate;
                    if (ReplyRecoveryCallback != null)
                    {
                        adapterListener.RecoveryCallback = ReplyRecoveryCallback;
                    }
                }

                if (DefaultRequeueRejected.HasValue)
                {
                    adapterListener.DefaultRequeueRejected = DefaultRequeueRejected.Value;
                }

                if (endpoint != null && endpoint.ReplyPostProcessor != null)
                {
                    adapterListener.ReplyPostProcessor = endpoint.ReplyPostProcessor;
                }
            }

            InitializeContainer(instance, endpoint);

            ContainerCustomizer?.Invoke(instance);

            return instance;
        }

        IMessageListenerContainer IRabbitListenerContainerFactory.CreateListenerContainer(IRabbitListenerEndpoint endpoint)
        {
            return CreateListenerContainer(endpoint);
        }

        protected abstract C CreateContainerInstance();

        protected virtual void InitializeContainer(C instance, IRabbitListenerEndpoint endpoint)
        {
        }
    }
}
