﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Common.Retry;
using Steeltoe.Integration.Rabbit.Inbound;
using Steeltoe.Integration.Rabbit.Outbound;
using Steeltoe.Integration.Rabbit.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Support.PostProcessor;
using Steeltoe.Stream.Binder.Rabbit.Config;
using Steeltoe.Stream.Config;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Stream.Binder.Rabbit
{
    public partial class RabbitBinderTests : PartitionCapableBinderTests<RabbitTestBinder, RabbitMessageChannelBinder>, IDisposable
    {
#pragma warning disable IDE1006 // Naming Styles
        private const string TEST_PREFIX = "bindertest.";
#pragma warning restore IDE1006 // Naming Styles
        private static readonly string _bigExceptionMessage = new string('x', 10_000);

        private RabbitTestBinder _testBinder;
        private CachingConnectionFactory _cachingConnectionFactory;

        private int _maxStackTraceSize;

        public RabbitBinderTests(ITestOutputHelper output)
            : base(output, new XunitLoggerFactory(output))
        {
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Output.WriteLine("running dispose ...");
            Cleanup();
        }

        public Spy SpyOn(string queue)
        {
            var template = new RabbitTemplate(GetResource());
            template.SetAfterReceivePostProcessors(new DelegatingDecompressingPostProcessor());
            return new Spy
            {
                Receive = (expectNull) =>
                {
                    if (expectNull)
                    {
                        Thread.Sleep(50);
                        return template.ReceiveAndConvert<object>(new RabbitConsumerOptions().Prefix + queue);
                    }

                    object bar = null;
                    var n = 0;

                    while (n++ < 100 && bar == null)
                    {
                        bar = template.ReceiveAndConvert<object>(new RabbitConsumerOptions().Prefix + queue);
                        Thread.Sleep(100);
                    }

                    Assert.True(n < 100, "Message did not arrive in RabbitMQ");
                    return bar;
                }
            };
        }

        public RabbitTestBinder GetBinder(RabbitBindingsOptions rabbitBindingsOptions)
        {
            if (_testBinder == null)
            {
                var options = new RabbitOptions
                {
                    PublisherReturns = true
                };
                _cachingConnectionFactory = GetResource();
                _testBinder = new RabbitTestBinder(_cachingConnectionFactory, options, new RabbitBinderOptions(), rabbitBindingsOptions, LoggerFactory);
            }

            return _testBinder;
        }

        protected ConsumerOptions GetConsumerOptions(string bindingName, RabbitBindingsOptions bindingsOptions, RabbitConsumerOptions rabbitConsumerOptions = null, RabbitBindingOptions bindingOptions = null)
        {
            rabbitConsumerOptions ??= new RabbitConsumerOptions();
            rabbitConsumerOptions.PostProcess();

            bindingOptions ??= new RabbitBindingOptions();
            bindingOptions.Consumer = rabbitConsumerOptions;
            bindingsOptions.Bindings.Add(bindingName, bindingOptions);

            var consumerOptions = new ConsumerOptions() { BindingName = bindingName };
            consumerOptions.PostProcess(bindingName);
            return consumerOptions;
        }

        protected ProducerOptions GetProducerOptions(string bindingName, RabbitBindingsOptions bindingsOptions, RabbitBindingOptions bindingOptions = null)
        {
            var rabbitProducerOptions = new RabbitProducerOptions();
            rabbitProducerOptions.PostProcess();

            bindingOptions ??= new RabbitBindingOptions();

            bindingOptions.Producer = rabbitProducerOptions;
            bindingsOptions.Bindings.Add(bindingName, bindingOptions);

            var producerOptions = new ProducerOptions() { BindingName = bindingName };
            producerOptions.PostProcess(bindingName);
            return producerOptions;
        }

        protected override RabbitTestBinder GetBinder()
        {
            if (_testBinder == null)
            {
                var options = new RabbitOptions
                {
                    PublisherReturns = true
                };
                _cachingConnectionFactory = GetResource();
                _testBinder = new RabbitTestBinder(_cachingConnectionFactory, options, new RabbitBinderOptions(), new RabbitBindingsOptions(), LoggerFactory);
            }

            return _testBinder;
        }

        private DirectMessageListenerContainer VerifyContainer(RabbitInboundChannelAdapter endpoint)
        {
            DirectMessageListenerContainer container;
            RetryTemplate retry;
            container = GetPropertyValue<DirectMessageListenerContainer>(endpoint, "MessageListenerContainer");

            Assert.Equal(AcknowledgeMode.NONE, container.AcknowledgeMode);
            Assert.StartsWith("foo.props.0", container.GetQueueNames()[0]);
            Assert.False(container.IsChannelTransacted);
            Assert.Equal(2, container.ConsumersPerQueue);
            Assert.False(container.DefaultRequeueRejected);
            Assert.Equal(20, container.PrefetchCount);

            retry = endpoint.RetryTemplate;
            Assert.Equal(23, GetFieldValue<int>(retry, "_maxAttempts"));
            Assert.Equal(2000, GetFieldValue<int>(retry, "_backOffInitialInterval"));
            Assert.Equal(20000, GetFieldValue<int>(retry, "_backOffMaxInterval"));
            Assert.Equal(5.0, GetFieldValue<double>(retry, "_backOffMultiplier"));

            return container;
        }

        private Exception BigCause(Exception innerException = null)
        {
            try
            {
                var capturedException = innerException ?? new Exception(_bigExceptionMessage);
                ExceptionDispatchInfo.Capture(capturedException).Throw();
            }
            catch (Exception ex)
            {
                if (ex.StackTrace != null && ex.StackTrace.Length > _maxStackTraceSize)
                {
                    return ex;
                }

                innerException = ex;
            }

            return BigCause(innerException);
        }

        private class TestMessageHandler : IMessageHandler
        {
            public Action<IMessage> OnHandleMessage { get; set; }

            public string ServiceName { get => "TestMessageHandler"; set => throw new NotImplementedException(); }

            public void HandleMessage(IMessage message) => OnHandleMessage.Invoke(message);
        }

        private void Cleanup()
        {
            if (_testBinder != null)
            {
                Cleanup(_testBinder);
            }

            if (_cachingConnectionFactory != null)
            {
                _cachingConnectionFactory.ResetConnection();
                _cachingConnectionFactory.Destroy();
                _cachingConnectionFactory = null;
            }

            _testBinder = null;
        }

        private void Cleanup(RabbitTestBinder binder)
        {
            binder.Cleanup();
            binder.CoreBinder.ConnectionFactory.Destroy();
            _cachingConnectionFactory = null;
        }

        private CachingConnectionFactory GetResource()
        {
            if (_cachingConnectionFactory == null)
            {
                _cachingConnectionFactory = new CachingConnectionFactory("localhost");
                _cachingConnectionFactory.CreateConnection().Close();
            }

            return _cachingConnectionFactory;
        }

        public class WrapperAccessor : RabbitOutboundEndpoint
        {
            public WrapperAccessor(IApplicationContext context, RabbitTemplate template)
                : base(context, template, null)
            {
            }

            public CorrelationData GetWrapper(IMessage message)
            {
                // var constructor = typeof(CorrelationDataWrapper).GetConstructor(typeof(string), typeof(object), typeof(Message));
                return Activator.CreateInstance(typeof(CorrelationDataWrapper), null, message, message) as CorrelationData;
            }
        }

        public class TestPartitionSupport : IPartitionKeyExtractorStrategy, IPartitionSelectorStrategy
        {
            public TestPartitionSupport(string serviceName)
            {
                ServiceName = serviceName;
            }

            public string ServiceName { get; set; }

            public object ExtractKey(IMessage message)
            {
                return message.Payload;
            }

            public int SelectPartition(object key, int partitionCount)
            {
                return (int)key;
            }
        }
    }
}
