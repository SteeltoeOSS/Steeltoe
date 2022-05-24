// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Retry;
using Steeltoe.Integration.Rabbit.Inbound;
using Steeltoe.Integration.Rabbit.Outbound;
using Steeltoe.Messaging;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Support.PostProcessor;
using Steeltoe.Stream.Binder.Rabbit.Config;
using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Stream.Binder.Rabbit
{
    public partial class RabbitBinderTests : PartitionCapableBinderTests<RabbitTestBinder, RabbitMessageChannelBinder>, IDisposable
    {
        private const string TEST_PREFIX = "bindertest.";
        private static readonly string _bigExceptionMessage = new ('x', 10_000);
        private bool _disposed;

        private RabbitTestBinder _testBinder;

        private int _maxStackTraceSize;

        public RabbitBinderTests(ITestOutputHelper output)
            : base(output, new XunitLoggerFactory(output))
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Spy SpyOn(string queue)
        {
            var template = new RabbitTemplate(GetResource());
            template.SetAfterReceivePostProcessors(new DelegatingDecompressingPostProcessor());
            return new Spy
            {
                Receive = expectNull =>
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

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Cleanup();
                }

                _disposed = true;
            }
        }

        protected override RabbitTestBinder GetBinder(RabbitBindingsOptions rabbitBindingsOptions = null)
        {
            if (_testBinder == null)
            {
                var options = new RabbitOptions
                {
                    PublisherReturns = true
                };
                var ccf = GetResource();
                var rabbitOptions = new TestOptionsMonitor<RabbitOptions>(options);
                var binderOptions = new TestOptionsMonitor<RabbitBinderOptions>(null);
                var bindingsOptions = new TestOptionsMonitor<RabbitBindingsOptions>(rabbitBindingsOptions ?? new RabbitBindingsOptions());

                _testBinder = new RabbitTestBinder(ccf, rabbitOptions, binderOptions, bindingsOptions, LoggerFactory);
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

        private void Cleanup()
        {
            if (_testBinder != null)
            {
                Cleanup(_testBinder);
            }

            if (CachingConnectionFactory != null)
            {
                CachingConnectionFactory.ResetConnection();
                CachingConnectionFactory.Destroy();
                CachingConnectionFactory = null;
            }

            _testBinder = null;
        }

        private void Cleanup(RabbitTestBinder binder)
        {
            binder.Cleanup();
            binder.CoreBinder.ConnectionFactory.Destroy();
            CachingConnectionFactory = null;
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
