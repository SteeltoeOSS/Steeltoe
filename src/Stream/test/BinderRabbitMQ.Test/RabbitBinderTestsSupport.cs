using EasyNetQ.Management.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Common.Retry;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Rabbit.Inbound;
using Steeltoe.Integration.Rabbit.Outbound;
using Steeltoe.Messaging;
using Steeltoe.Messaging.RabbitMQ;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Retry;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Binder.Rabbit.Config;
using Steeltoe.Stream.Binder.Rabbit.Provisioning;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Converter;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Stream.Binder.Rabbit
{
    public partial class RabbitBinderTests : PartitionCapableBinderTests<RabbitTestBinder, RabbitMessageChannelBinder>, IDisposable
    {
        private RabbitTestBinder _testBinder;
        private CachingConnectionFactory _cachingConnectionFactory;
        private readonly string TEST_PREFIX = "bindertest.";
        private static string BIG_EXCEPTION_MESSAGE = new string('x', 10_000);
        private int maxStackTraceSize;

        public RabbitBinderTests(ITestOutputHelper output) : base(output)
        {
        }

        public void Dispose()
        {
            Cleanup(GetBinder());
        }
      

        protected override ConsumerOptions CreateConsumerOptions()
        {
            return new ExtendedConsumerOptions<RabbitConsumerOptions>(new RabbitConsumerOptions());
        }

        protected override ProducerOptions CreateProducerOptions()
        {
            return new ExtendedProducerOptions<RabbitProducerOptions>(new RabbitProducerOptions());
        }

        protected override RabbitTestBinder GetBinder()
        {
            if (_testBinder == null)
            {
                var options = new RabbitOptions();
                //  options.PublisherConfirms(ConfirmType.SIMPLE);
                options.PublisherReturns = true;
                _cachingConnectionFactory = GetResource();
                _testBinder = new RabbitTestBinder(_cachingConnectionFactory, options, new RabbitBinderOptions(), new RabbitBindingsOptions());
            }

            return _testBinder;
        }

        //protected RabbitTestBinder GetBinder(RabbitConsumerOptions consumerOptions)
        //{
        //    if (_testBinder == null)
        //    {
        //        var options = new RabbitOptions();
        //        //  options.PublisherConfirms(ConfirmType.SIMPLE);
        //        options.PublisherReturns = true;
        //        _cachingConnectionFactory = GetResource();
        //        var bindingsOptions = new RabbitBindingsOptions();
        //        var consumerBindingOptions = new RabbitBindingOptions();
        //        consumerBindingOptions.Consumer = consumerOptions;

        //      //  bindingsOptions.Bindings.Add(string.Empty, consumerBindingOptions);
        //        _testBinder = new RabbitTestBinder(_cachingConnectionFactory, options, new RabbitBinderOptions(), bindingsOptions);
        //    }

        //    return _testBinder;
        //}

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

        private void VerifyFooRequestProducer(ILifecycle endpoint)
        {
            //         var requestMatchers =  TestUtils.getPropertyValue(endpoint,
            //                  "headerMapper.requestHeaderMatcher.matchers", List.class);
            //assertThat(requestMatchers).hasSize(4);
            //      assertThat(TestUtils.getPropertyValue(requestMatchers.get(3), "pattern"))
            //		.isEqualTo("foo");
        }

        private Exception BigCause(Exception innerException = null)
        {
         //   Exception innerException = null;
            try
            {
                var capturedException = innerException ?? new Exception(BIG_EXCEPTION_MESSAGE);
                ExceptionDispatchInfo.Capture(capturedException).Throw();
            }
            catch (Exception ex)
            {
                if (ex.StackTrace != null && ex.StackTrace.Length > this.maxStackTraceSize)
                {
                    return ex;
                }

                innerException = ex;
            }

            return BigCause(innerException);
        }

        private class TestMessageHandler : IMessageHandler
        {
            public string ServiceName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public Action<IMessage> OnHandleMessage { get; set; }

            public void HandleMessage(IMessage message) => OnHandleMessage.Invoke(message);
         
        }

        private void Cleanup(RabbitTestBinder binder)
        {
            binder.CoreBinder.ConnectionFactory.Destroy();
            binder.Cleanup();
            _cachingConnectionFactory = null;
        }

        private CachingConnectionFactory GetResource(bool management=false)
        {
            if (_cachingConnectionFactory == null)
            {

                _cachingConnectionFactory = new CachingConnectionFactory("localhost");
                _cachingConnectionFactory.CreateConnection().Close(); // why?

                if (management)
                {
                    // var socket = SocketFactory
                }
            }

            return _cachingConnectionFactory;
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

        //public class TestPartitionSelectorClass : IPartitionSelectorStrategy
        //{
        //    public TestPartitionSelectorClass(string serviceName)
        //    {
        //        ServiceName = serviceName;
        //    }

        //    public string ServiceName { get; set; }

        //    public int SelectPartition(object key, int partitionCount)
        //    {
        //        return (int)key;
        //    }
        //}
    }
}
