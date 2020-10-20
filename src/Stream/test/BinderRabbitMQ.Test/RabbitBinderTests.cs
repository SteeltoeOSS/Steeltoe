using RabbitMQ.Client;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Common.Retry;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Rabbit.Inbound;
using Steeltoe.Messaging;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Binder.Rabbit.Config;
using Steeltoe.Stream.Binder.Rabbit.Provisioning;
using Steeltoe.Stream.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using IConnectionFactory = Steeltoe.Messaging.RabbitMQ.Connection.IConnectionFactory;

namespace Steeltoe.Stream.Binder.Rabbit
{
    public class RabbitBinderTests : PartitionCapableBinderTests<RabbitTestBinder, RabbitMessageChannelBinder>
    {
        private RabbitTestBinder _testBinder;
        private CachingConnectionFactory _cachingConnectionFactory;
        public RabbitBinderTests(ITestOutputHelper output) : base(output)
        {
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
                _cachingConnectionFactory = GetResource(false);
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
        //        _cachingConnectionFactory = GetResource(false);
        //        var bindingsOptions = new RabbitBindingsOptions();
        //        var consumerBindingOptions = new RabbitBindingOptions();
        //        consumerBindingOptions.Consumer = consumerOptions;

        //      //  bindingsOptions.Bindings.Add(string.Empty, consumerBindingOptions);
        //        _testBinder = new RabbitTestBinder(_cachingConnectionFactory, options, new RabbitBinderOptions(), bindingsOptions);
        //    }

        //    return _testBinder;
        //}

        private CachingConnectionFactory GetResource(bool management)
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


        [Fact]
        public void TestSendAndReceiveBad()
        {
            var ccf = GetResource(false);
            RabbitTestBinder binder = GetBinder();

            var moduleOutputChannel = CreateBindableChannel("output", new BindingOptions());
            var moduleInputChannel = CreateBindableChannel("input", new BindingOptions());
            var producerBinding = binder.BindProducer("bad.0", moduleOutputChannel, CreateProducerOptions());

            var endpoint = GetFieldValue<Integration.Rabbit.Outbound.RabbitOutboundEndpoint>(producerBinding, "_lifecycle");

            Assert.True(endpoint.HeadersMappedLast);
            Assert.Contains("Passthrough", endpoint.Template.MessageConverter.GetType().Name);

            var consumerProps = (ExtendedConsumerOptions<RabbitConsumerOptions>)CreateConsumerOptions();
            consumerProps.Extension.ContainerType = ContainerType.DIRECT;

            var consumerBinding = binder.BindConsumer("bad.0", "test", moduleInputChannel, consumerProps);

            var inbound = GetFieldValue<Integration.Rabbit.Inbound.RabbitInboundChannelAdapter>(consumerBinding, "_lifecycle");
            Assert.Contains("Passthrough", inbound.MessageConverter.GetType().Name);
            var container = GetPropertyValue<DirectMessageListenerContainer>(inbound, "MessageListenerContainer");
            Assert.NotNull(container);

            var message = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("bad"))
                .SetHeader(MessageHeaders.CONTENT_TYPE, "foo/bar")
                .Build();

            var handler = new TestMessageHandler(3);
            moduleInputChannel.Subscribe(handler);
            moduleOutputChannel.Send(message);

            Assert.True(handler.Wait());

            producerBinding.Unbind();
            consumerBinding.Unbind();
            Cleanup(binder);
        }

        [Fact]
        public void TestProducerErrorChannel()
        {
            /// ccf.IsPublisherConfirms = true; 
            /// 
            var ccf = GetResource(false);
            ccf.PublisherConfirmType = CachingConnectionFactory.ConfirmType.CORRELATED;
            ccf.ResetConnection();
            RabbitTestBinder binder = GetBinder();

            //		CachingConnectionFactory ccf = this.rabbitAvailableRule.getResource();
            //        ccf.PublisherReturns = true;
            //		ccf.PublisherConfirmType = ConfirmType.CORRELATED;
            //		ccf.resetConnection();

            var moduleOutputChannel = CreateBindableChannel("output", new BindingOptions());
            //		DirectChannel moduleOutputChannel = createBindableChannel("output",
            //                new BindingProperties());
            //ExtendedProducerProperties<RabbitProducerProperties> producerProps = createProducerProperties();
            var producerOptions = CreateProducerOptions();
            producerOptions.ErrorChannelEnabled = true;

            //        producerProps.ErrorChannelEnabled = true;


            //		Binding<MessageChannel> producerBinding = binder.bindProducer("ec.0",
            //                moduleOutputChannel, producerProps);

            var producerBinding = binder.BindProducer("ec.0", moduleOutputChannel, producerOptions);

            //        final Message<?> message = MessageBuilder.withPayload("bad".getBytes())
            //                .Header = MessageHeaders.CONTENT_TYPE, "foo/bar").build(;
            var message = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("bad"))
               .SetHeader(MessageHeaders.CONTENT_TYPE, "foo/bar")
               .Build();

            //        SubscribableChannel ec = binder.getApplicationContext().getBean("ec.0.errors",
            //                SubscribableChannel.class);
            var ec = binder.ApplicationContext.GetService<PublishSubscribeChannel>("ec.0.errors");
            Assert.NotNull(ec);
            //		final AtomicReference<Message<?>> errorMessage = new AtomicReference<>();
            var errorMessage = new AtomicReference<IMessage>();
            //        final CountDownLatch latch = new CountDownLatch(2);
            var handler = new TestMessageHandler(1);
            ec.Subscribe(handler);
            //ec.Subscribe(new MessageHandler()
            //        {

            //            @Override

            //            public void handleMessage(Message<?> message) throws MessagingException {
            //    errorMessage.set(message);
            //    latch.countDown();
            //}

            //});
            //    		SubscribableChannel globalEc = binder.getApplicationContext().getBean(
            //                    IntegrationContextUtils.ERROR_CHANNEL_BEAN_NAME,
            //                    SubscribableChannel.class);
            //    		globalEc.subscribe(new MessageHandler()
            //{

            //    @Override

            //                public void handleMessage(Message<?> message) throws MessagingException {
            //        latch.countDown();
            //    }
       //     var globalHandler = new TestMessageHandler(1);


        //});
           
            moduleOutputChannel.Send(message);
            Assert.True(handler.Wait());
            //        assertThat(errorMessage.get()).isInstanceOf(ErrorMessage.class);
            //		assertThat(errorMessage.get().getPayload())
            //				.isInstanceOf(ReturnedAmqpMessageException.class);
            //		ReturnedAmqpMessageException exception = (ReturnedAmqpMessageException)errorMessage
            //                .get().getPayload();
            //        assertThat(exception.getReplyCode()).isEqualTo(312);
            //        assertThat(exception.getReplyText()).isEqualTo("NO_ROUTE");

            //        AmqpOutboundEndpoint endpoint = TestUtils.getPropertyValue(producerBinding,
            //                "lifecycle", AmqpOutboundEndpoint.class);
            //		assertThat(TestUtils.getPropertyValue(endpoint,
            //				"confirmCorrelationExpression.expression")).isEqualTo("#root");
            //        class WrapperAccessor extends AmqpOutboundEndpoint
            //        {

            //            WrapperAccessor(AmqpTemplate amqpTemplate) {
            //				super(amqpTemplate);
            //    }

            //    CorrelationDataWrapper getWrapper() throws Exception
            //    {
            //        Constructor<CorrelationDataWrapper> constructor = CorrelationDataWrapper.class
            //						.getDeclaredConstructor(String.class, Object.class,
            //								Message.class);
            //				ReflectionUtils.makeAccessible(constructor);
            //				return constructor.newInstance(null, message, message);
            //			}

            //		}
            //		endpoint.confirm(new WrapperAccessor(mock(AmqpTemplate.class)).getWrapper(),
            //				false, "Mock NACK");
            //assertThat(errorMessage.get()).isInstanceOf(ErrorMessage.class);
            //assertThat(errorMessage.get().getPayload())
            //        .isInstanceOf(NackedAmqpMessageException.class);
            //NackedAmqpMessageException nack = (NackedAmqpMessageException)errorMessage.get()
            //        .getPayload();
            //assertThat(nack.getNackReason()).isEqualTo("Mock NACK");
            //assertThat(nack.getCorrelationData()).isEqualTo(message);
            //assertThat(nack.getFailedMessage()).isEqualTo(message);
            //producerBinding.unbind();

        }
    
        [Fact]
        public void TestProducerAckChannel()
        {
//            RabbitTestBinder binder = getBinder();
//		CachingConnectionFactory ccf = this.rabbitAvailableRule.getResource();
//        ccf.PublisherReturns = true;
//		ccf.PublisherConfirmType = ConfirmType.CORRELATED;
//		ccf.resetConnection();
//		DirectChannel moduleOutputChannel = createBindableChannel("output",
//                new BindingProperties());
//        ExtendedProducerProperties<RabbitProducerProperties> producerProps = createProducerProperties();
//        producerProps.ErrorChannelEnabled = true;
//		producerProps.Extension.ConfirmAckChannel = "acksChannel";
//        Binding<MessageChannel> producerBinding = binder.bindProducer("acks.0",
//                moduleOutputChannel, producerProps);
//        final Message<?> message = MessageBuilder.withPayload("acksMessage".getBytes())
//                .build();
//        final AtomicReference<Message<?>> confirm = new AtomicReference<>();
//        final CountDownLatch confirmLatch = new CountDownLatch(1);
//        binder.getApplicationContext().getBean("acksChannel", DirectChannel.class)
//				.subscribe(m -> {
//            confirm.set(m);
//            confirmLatch.countDown();
//        });
//		moduleOutputChannel.send(message);
//		assertThat(confirmLatch.await(10, TimeUnit.SECONDS)).isTrue();
//        assertThat(confirm.get().getPayload()).isEqualTo("acksMessage".getBytes());
//        producerBinding.unbind();
	}

        //    @Test
        //    public void testProducerConfirmHeader() throws Exception
        //    {
        //        RabbitTestBinder binder = getBinder();
        //        CachingConnectionFactory ccf = this.rabbitAvailableRule.getResource();
        //        ccf.PublisherReturns = true;
        //        ccf.PublisherConfirmType = ConfirmType.CORRELATED;
        //        ccf.resetConnection();
        //        DirectChannel moduleOutputChannel = createBindableChannel("output",

        //                new BindingProperties());
        //		ExtendedProducerProperties<RabbitProducerProperties> producerProps = createProducerProperties();
        //    producerProps.Extension.UseConfirmHeader = true;
        //    Binding<MessageChannel> producerBinding = binder.bindProducer("confirms.0",
        //            moduleOutputChannel, producerProps);
        //    CorrelationData correlation = new CorrelationData("testConfirm");
        //    final Message<?> message = MessageBuilder.withPayload("confirmsMessage".getBytes())
        //            .Header = AmqpHeaders.PUBLISH_CONFIRM_CORRELATION, correlation
        //            .build();
        //    moduleOutputChannel.send(message);
        //		Confirm confirm = correlation.getFuture().get(10, TimeUnit.SECONDS);
        //    assertThat(confirm.isAck()).isTrue();
        //    assertThat(correlation.getReturnedMessage()).isNotNull();
        //    producerBinding.unbind();
        //	}


        [Fact]
        public void TestConsumerProperties()
        {
            var consumerOptions = new RabbitConsumerOptions();
            consumerOptions.RequeueRejected = true;
            consumerOptions.Transacted = true;
            consumerOptions.Exclusive = true;
            consumerOptions.MissingQueuesFatal = true;
            consumerOptions.FailedDeclarationRetryInterval = 1500L;
            consumerOptions.QueueDeclarationRetries = 23;

            var binder = GetBinder();

            var properties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            properties.Extension = consumerOptions;

            var consumerBinding = binder.BindConsumer("props.0", null, CreateBindableChannel("input", new BindingOptions()), properties);

            var endpoint = ExtractEndpoint(consumerBinding);
            Assert.NotNull(endpoint);
            var container = GetPropertyValue<DirectMessageListenerContainer>(endpoint, "MessageListenerContainer");
            Assert.NotNull(container);
            Assert.Equal(AcknowledgeMode.AUTO, container.AcknowledgeMode);
            Assert.StartsWith(properties.Extension.Prefix, container.GetQueueNames()[0]);
            Assert.True(container.Exclusive);
            Assert.True(container.IsChannelTransacted);
            Assert.True(container.Exclusive);
            Assert.True(container.DefaultRequeueRejected);
            Assert.Equal(1, container.PrefetchCount);
            Assert.True(container.MissingQueuesFatal);
            Assert.Equal(1500L, container.FailedDeclarationRetryInterval);

            var retry = endpoint.RetryTemplate;
            Assert.NotNull(retry);
            Assert.Equal(3, GetFieldValue<int>(retry, "_maxAttempts"));
            Assert.Equal(1000, GetFieldValue<int>(retry, "_backOffInitialInterval"));
            Assert.Equal(10000, GetFieldValue<int>(retry, "_backOffMaxInterval"));
            Assert.Equal(2.0, GetFieldValue<double>(retry, "_backOffMultiplier"));
            consumerBinding.Unbind();
            Assert.False(endpoint.IsRunning);

            properties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            properties.Extension.AcknowledgeMode = AcknowledgeMode.NONE;
            properties.BackOffInitialInterval = 2000;
            properties.BackOffMaxInterval = 20000;
            properties.BackOffMultiplier = 5.0;
            properties.Concurrency = 2;
            properties.MaxAttempts = 23;
            properties.Extension.MaxConcurrency = 3;
            properties.Extension.Prefix = "foo.";
            properties.Extension.Prefetch = 20;
            properties.Extension.HeaderPatterns = new string[] { "foo" }.ToList();
            properties.Extension.BatchSize = 10;
            var quorum = properties.Extension.Quorum;
            quorum.Enabled = true;
            quorum.DeliveryLimit = 10;
            quorum.InitialQuorumSize = 1;
            properties.InstanceIndex = 0;
            consumerBinding = binder.BindConsumer("props.0", "test", CreateBindableChannel("input", new BindingOptions()), properties);

            endpoint = ExtractEndpoint(consumerBinding);
            container = VerifyContainer(endpoint);

            Assert.Equal("foo.props.0.test", container.GetQueueNames()[0]);

            consumerBinding.Unbind();
            Assert.False(endpoint.IsRunning);
        }

        [Fact]
        public void TestMultiplexOnPartitionedConsumerWithMultipleDestinations()
        {
            var consumerProperties = CreateConsumerOptions();
            var proxy = new RabbitProxy();
            var port = proxy.Port;
            var ccf = new CachingConnectionFactory("localhost", port);

            var rabbitExchangeQueueProvisioner = new RabbitExchangeQueueProvisioner(ccf, new RabbitBindingsOptions());

            consumerProperties.Multiplex = true;
            consumerProperties.Partitioned = true;
            consumerProperties.InstanceIndexList = new int[] { 1, 2, 3 }.ToList();

            var consumerDestination = rabbitExchangeQueueProvisioner.ProvisionConsumerDestination("foo,qaa", "boo", consumerProperties);

            proxy.Stop();


            Assert.Equal("foo.boo-1,foo.boo-2,foo.boo-3,qaa.boo-1,qaa.boo-2,qaa.boo-3", consumerDestination.Name);
        }

    //	@Test
    //    public void testConsumerPropertiesWithUserInfrastructureNoBind() throws Exception
    //{
    //    RabbitAdmin admin = new RabbitAdmin(this.rabbitAvailableRule.getResource());
    //Queue queue = new Queue("propsUser1.infra");
    //admin.declareQueue(queue);
    //DirectExchange exchange = new DirectExchange("propsUser1");
    //admin.declareExchange(exchange);
    //admin.declareBinding(BindingBuilder.bind(queue).to(exchange).with("foo"));

    //RabbitTestBinder binder = getBinder();
    //ExtendedConsumerProperties<RabbitConsumerProperties> properties = createConsumerProperties();
    //properties.Extension.DeclareExchange = false;
    //properties.Extension.BindQueue = false;

    //Binding<MessageChannel> consumerBinding = binder.bindConsumer("propsUser1",
    //        "infra", createBindableChannel("input", new BindingProperties()),
    //        properties);
    //Lifecycle endpoint = extractEndpoint(consumerBinding);
    //SimpleMessageListenerContainer container = TestUtils.getPropertyValue(endpoint,
    //        "messageListenerContainer", SimpleMessageListenerContainer.class);
    //assertThat(TestUtils.getPropertyValue(container, "missingQueuesFatal",
    //        Boolean.class)).isFalse();
    //assertThat(container.isRunning()).isTrue();
    //consumerBinding.unbind();
    //assertThat(container.isRunning()).isFalse();
    //Client client = new Client("http://guest:guest@localhost:15672/api/");
    //List <?> bindings = client.getBindingsBySource("/", exchange.getName());
    //assertThat(bindings.size()).isEqualTo(1);
    //	}

    //	@Test
    //    public void testAnonWithBuiltInExchange() throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    ExtendedConsumerProperties<RabbitConsumerProperties> properties = createConsumerProperties();
    //    properties.Extension.DeclareExchange = false;
    //    properties.Extension.QueueNameGroupOnly = true;

    //    Binding<MessageChannel> consumerBinding = binder.bindConsumer("amq.topic", null,
    //				createBindableChannel("input", new BindingProperties()), properties);
    //Lifecycle endpoint = extractEndpoint(consumerBinding);
    //SimpleMessageListenerContainer container = TestUtils.getPropertyValue(endpoint,
    //        "messageListenerContainer", SimpleMessageListenerContainer.class);
    //String queueName = container.getQueueNames()[0];
    //assertThat(queueName).startsWith("anonymous.");
    //assertThat(container.isRunning()).isTrue();
    //consumerBinding.unbind();
    //assertThat(container.isRunning()).isFalse();
    //	}

    //	@Test
    //    public void testAnonWithBuiltInExchangeCustomPrefix() throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    ExtendedConsumerProperties<RabbitConsumerProperties> properties = createConsumerProperties();
    //    properties.Extension.DeclareExchange = false;
    //    properties.Extension.QueueNameGroupOnly = true;
    //    properties.Extension.AnonymousGroupPrefix = "customPrefix.";

    //    Binding<MessageChannel> consumerBinding = binder.bindConsumer("amq.topic", null,
    //				createBindableChannel("input", new BindingProperties()), properties);
    //Lifecycle endpoint = extractEndpoint(consumerBinding);
    //SimpleMessageListenerContainer container = TestUtils.getPropertyValue(endpoint,
    //        "messageListenerContainer", SimpleMessageListenerContainer.class);
    //String queueName = container.getQueueNames()[0];
    //assertThat(queueName).startsWith("customPrefix.");
    //assertThat(container.isRunning()).isTrue();
    //consumerBinding.unbind();
    //assertThat(container.isRunning()).isFalse();
    //	}

    //	@Test
    //    public void testConsumerPropertiesWithUserInfrastructureCustomExchangeAndRK()

    //            throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    ExtendedConsumerProperties<RabbitConsumerProperties> properties = createConsumerProperties();
    //    properties.Extension.ExchangeType = ExchangeTypes.DIRECT;
    //    properties.Extension.BindingRoutingKey = "foo,bar";
    //    properties.Extension.BindingRoutingKeyDelimiter = ",";
    //    properties.Extension.QueueNameGroupOnly = true;
    //    // properties.Extension.DelayedExchange = true; // requires delayed message
    //    // exchange plugin; tested locally

    //    String group = "infra";
    //    Binding<MessageChannel> consumerBinding = binder.bindConsumer("propsUser2", group,
    //				createBindableChannel("input", new BindingProperties()), properties);
    //Lifecycle endpoint = extractEndpoint(consumerBinding);
    //SimpleMessageListenerContainer container = TestUtils.getPropertyValue(endpoint,
    //        "messageListenerContainer", SimpleMessageListenerContainer.class);
    //assertThat(container.isRunning()).isTrue();
    //consumerBinding.unbind();
    //assertThat(container.isRunning()).isFalse();
    //assertThat(container.getQueueNames()[0]).isEqualTo(group);
    //Client client = new Client("http://guest:guest@localhost:15672/api/");
    //List<BindingInfo> bindings = client.getBindingsBySource("/", "propsUser2");
    //int n = 0;
    //while (n++ < 100 && bindings == null || bindings.size() < 1)
    //{
    //    Thread.sleep(100);
    //    bindings = client.getBindingsBySource("/", "propsUser2");
    //}
    //assertThat(bindings.size()).isEqualTo(2);
    //assertThat(bindings.get(0).getSource()).isEqualTo("propsUser2");
    //assertThat(bindings.get(0).getDestination()).isEqualTo(group);
    //assertThat(bindings.get(0).getRoutingKey()).isIn("foo", "bar");
    //assertThat(bindings.get(1).getSource()).isEqualTo("propsUser2");
    //assertThat(bindings.get(1).getDestination()).isEqualTo(group);
    //assertThat(bindings.get(1).getRoutingKey()).isIn("foo", "bar");
    //assertThat(bindings.get(1).getRoutingKey()).isNotEqualTo(bindings.get(0).getRoutingKey());

    //ExchangeInfo exchange = client.getExchange("/", "propsUser2");
    //while (n++ < 100 && exchange == null)
    //{
    //    Thread.sleep(100);
    //    exchange = client.getExchange("/", "propsUser2");
    //}
    //assertThat(exchange.getType()).isEqualTo("direct");
    //assertThat(exchange.isDurable()).isEqualTo(true);
    //assertThat(exchange.isAutoDelete()).isEqualTo(false);
    //	}

    //	@Test
    //    public void testConsumerPropertiesWithUserInfrastructureCustomQueueArgs()

    //            throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    ExtendedConsumerProperties<RabbitConsumerProperties> properties = createConsumerProperties();
    //    RabbitConsumerProperties extProps = properties.Extension;
    //    extProps.ExchangeType = ExchangeTypes.DIRECT;
    //    extProps.ExchangeDurable = false;
    //    extProps.ExchangeAutoDelete = true;
    //    extProps.BindingRoutingKey = "foo";
    //    extProps.Expires = 30_000;
    //    extProps.Lazy = true;
    //    extProps.MaxLength = 10_000;
    //    extProps.MaxLengthBytes = 100_000;
    //    extProps.MaxPriority = 10;
    //    extProps.OverflowBehavior = "drop-head";
    //    extProps.Ttl = 2_000;
    //    extProps.AutoBindDlq = true;
    //    extProps.DeadLetterQueueName = "customDLQ";
    //    extProps.DeadLetterExchange = "customDLX";
    //    extProps.DeadLetterExchangeType = ExchangeTypes.TOPIC;
    //    extProps.DeadLetterRoutingKey = "customDLRK";
    //    extProps.DlqDeadLetterExchange = "propsUser3";
    //    // GH-259 - if the next line was commented, the test failed.
    //    extProps.DlqDeadLetterRoutingKey = "propsUser3";
    //    extProps.DlqExpires = 60_000;
    //    extProps.DlqLazy = true;
    //    extProps.DlqMaxLength = 20_000;
    //    extProps.DlqMaxLengthBytes = 40_000;
    //    extProps.DlqOverflowBehavior = "reject-publish";
    //    extProps.DlqMaxPriority = 8;
    //    extProps.DlqTtl = 1_000;
    //    extProps.ConsumerTagPrefix = "testConsumerTag";
    //    extProps.Exclusive = true;

    //    Binding<MessageChannel> consumerBinding = binder.bindConsumer("propsUser3",
    //				"infra", createBindableChannel("input", new BindingProperties()),
    //				properties);
    //Lifecycle endpoint = extractEndpoint(consumerBinding);
    //SimpleMessageListenerContainer container = TestUtils.getPropertyValue(endpoint,
    //        "messageListenerContainer", SimpleMessageListenerContainer.class);
    //assertThat(container.isRunning()).isTrue();
    //Client client = new Client("http://guest:guest@localhost:15672/api");
    //List<BindingInfo> bindings = client.getBindingsBySource("/", "propsUser3");
    //int n = 0;
    //while (n++ < 100 && bindings == null || bindings.size() < 1)
    //{
    //    Thread.sleep(100);
    //    bindings = client.getBindingsBySource("/", "propsUser3");
    //}
    //assertThat(bindings.size()).isEqualTo(1);
    //assertThat(bindings.get(0).getSource()).isEqualTo("propsUser3");
    //assertThat(bindings.get(0).getDestination()).isEqualTo("propsUser3.infra");
    //assertThat(bindings.get(0).getRoutingKey()).isEqualTo("foo");

    //bindings = client.getBindingsBySource("/", "customDLX");
    //n = 0;
    //while (n++ < 100 && bindings == null || bindings.size() < 1)
    //{
    //    Thread.sleep(100);
    //    bindings = client.getBindingsBySource("/", "customDLX");
    //}
    ////		assertThat(bindings.size()).isEqualTo(1);
    //assertThat(bindings.get(0).getSource()).isEqualTo("customDLX");
    //assertThat(bindings.get(0).getDestination()).isEqualTo("customDLQ");
    //assertThat(bindings.get(0).getRoutingKey()).isEqualTo("customDLRK");

    //ExchangeInfo exchange = client.getExchange("/", "propsUser3");
    //n = 0;
    //while (n++ < 100 && exchange == null)
    //{
    //    Thread.sleep(100);
    //    exchange = client.getExchange("/", "propsUser3");
    //}
    //assertThat(exchange.getType()).isEqualTo("direct");
    //assertThat(exchange.isDurable()).isEqualTo(false);
    //assertThat(exchange.isAutoDelete()).isEqualTo(true);

    //exchange = client.getExchange("/", "customDLX");
    //n = 0;
    //while (n++ < 100 && exchange == null)
    //{
    //    Thread.sleep(100);
    //    exchange = client.getExchange("/", "customDLX");
    //}
    //assertThat(exchange.getType()).isEqualTo("topic");
    //assertThat(exchange.isDurable()).isEqualTo(true);
    //assertThat(exchange.isAutoDelete()).isEqualTo(false);

    //QueueInfo queue = client.getQueue("/", "propsUser3.infra");
    //n = 0;
    //while (n++ < 100 && queue == null || queue.getConsumerCount() == 0)
    //{
    //    Thread.sleep(100);
    //    queue = client.getQueue("/", "propsUser3.infra");
    //}
    //assertThat(queue).isNotNull();
    //Map<String, Object> args = queue.getArguments();
    //assertThat(args.get("x-expires")).isEqualTo(30_000);
    //assertThat(args.get("x-max-length")).isEqualTo(10_000);
    //assertThat(args.get("x-max-length-bytes")).isEqualTo(100_000);
    //assertThat(args.get("x-overflow")).isEqualTo("drop-head");
    //assertThat(args.get("x-max-priority")).isEqualTo(10);
    //assertThat(args.get("x-message-ttl")).isEqualTo(2_000);
    //assertThat(args.get("x-dead-letter-exchange")).isEqualTo("customDLX");
    //assertThat(args.get("x-dead-letter-routing-key")).isEqualTo("customDLRK");
    //assertThat(args.get("x-queue-mode")).isEqualTo("lazy");
    //assertThat(queue.getExclusiveConsumerTag()).isEqualTo("testConsumerTag#0");

    //queue = client.getQueue("/", "customDLQ");

    //n = 0;
    //while (n++ < 100 && queue == null)
    //{
    //    Thread.sleep(100);
    //    queue = client.getQueue("/", "customDLQ");
    //}
    //assertThat(queue).isNotNull();
    //args = queue.getArguments();
    //assertThat(args.get("x-expires")).isEqualTo(60_000);
    //assertThat(args.get("x-max-length")).isEqualTo(20_000);
    //assertThat(args.get("x-max-length-bytes")).isEqualTo(40_000);
    //assertThat(args.get("x-overflow")).isEqualTo("reject-publish");
    //assertThat(args.get("x-max-priority")).isEqualTo(8);
    //assertThat(args.get("x-message-ttl")).isEqualTo(1_000);
    //assertThat(args.get("x-dead-letter-exchange")).isEqualTo("propsUser3");
    //assertThat(args.get("x-dead-letter-routing-key")).isEqualTo("propsUser3");
    //assertThat(args.get("x-queue-mode")).isEqualTo("lazy");

    //consumerBinding.unbind();
    //assertThat(container.isRunning()).isFalse();
    //	}

    //	@Test
    //    public void testConsumerPropertiesWithHeaderExchanges() throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    ExtendedConsumerProperties<RabbitConsumerProperties> properties = createConsumerProperties();
    //    properties.Extension.ExchangeType = ExchangeTypes.HEADERS;
    //    properties.Extension.AutoBindDlq = true;
    //    properties.Extension.DeadLetterExchange = ExchangeTypes.HEADERS;
    //    properties.Extension.DeadLetterExchange = "propsHeader.dlx";
    //    Map<String, String> queueBindingArguments = new HashMap<>();
    //queueBindingArguments.put("x-match", "any");
    //queueBindingArguments.put("foo", "bar");
    //properties.Extension.QueueBindingArguments = queueBindingArguments;
    //properties.Extension.DlqBindingArguments = queueBindingArguments;

    //String group = "bindingArgs";
    //Binding<MessageChannel> consumerBinding = binder.bindConsumer("propsHeader", group,
    //        createBindableChannel("input", new BindingProperties()), properties);
    //Lifecycle endpoint = extractEndpoint(consumerBinding);
    //SimpleMessageListenerContainer container = TestUtils.getPropertyValue(endpoint,
    //        "messageListenerContainer", SimpleMessageListenerContainer.class);
    //assertThat(container.isRunning()).isTrue();
    //consumerBinding.unbind();
    //assertThat(container.isRunning()).isFalse();
    //assertThat(container.getQueueNames()[0]).isEqualTo("propsHeader." + group);
    //Client client = new Client("http://guest:guest@localhost:15672/api/");
    //List<BindingInfo> bindings = client.getBindingsBySource("/", "propsHeader");
    //int n = 0;
    //while (n++ < 100 && bindings == null || bindings.size() < 1)
    //{
    //    Thread.sleep(100);
    //    bindings = client.getBindingsBySource("/", "propsHeader");
    //}
    //assertThat(bindings.size()).isEqualTo(1);
    //assertThat(bindings.get(0).getSource()).isEqualTo("propsHeader");
    //assertThat(bindings.get(0).getDestination()).isEqualTo("propsHeader." + group);
    //assertThat(bindings.get(0).getArguments()).hasEntrySatisfying("x-match", v->assertThat(v).isEqualTo("any"));
    //assertThat(bindings.get(0).getArguments()).hasEntrySatisfying("foo", v->assertThat(v).isEqualTo("bar"));

    //bindings = client.getBindingsBySource("/", "propsHeader.dlx");
    //n = 0;
    //while (n++ < 100 && bindings == null || bindings.size() < 1)
    //{
    //    Thread.sleep(100);
    //    bindings = client.getBindingsBySource("/", "propsHeader.dlx");
    //}
    //assertThat(bindings.size()).isEqualTo(1);
    //assertThat(bindings.get(0).getSource()).isEqualTo("propsHeader.dlx");
    //assertThat(bindings.get(0).getDestination()).isEqualTo("propsHeader." + group + ".dlq");
    //assertThat(bindings.get(0).getArguments()).hasEntrySatisfying("x-match", v->assertThat(v).isEqualTo("any"));
    //assertThat(bindings.get(0).getArguments()).hasEntrySatisfying("foo", v->assertThat(v).isEqualTo("bar"));
    //	}

    //	@Test
    //    public void testProducerProperties() throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    Binding<MessageChannel> producerBinding = binder.bindProducer("props.0",
    //				createBindableChannel("input", new BindingProperties()),
    //				createProducerProperties());
    //Lifecycle endpoint = extractEndpoint(producerBinding);
    //MessageDeliveryMode mode = TestUtils.getPropertyValue(endpoint,
    //        "defaultDeliveryMode", MessageDeliveryMode.class);
    //assertThat(mode).isEqualTo(MessageDeliveryMode.PERSISTENT);
    //List <?> requestHeaders = TestUtils.getPropertyValue(endpoint,
    //        "headerMapper.requestHeaderMatcher.matchers", List.class);
    //assertThat(requestHeaders).hasSize(4);
    //producerBinding.unbind();
    //assertThat(endpoint.isRunning()).isFalse();
    //assertThat(TestUtils.getPropertyValue(endpoint, "amqpTemplate.transactional",
    //        Boolean.class)).isFalse();

    //ExtendedProducerProperties<RabbitProducerProperties> producerProperties = createProducerProperties();
    //this.applicationContext.registerBean("pkExtractor",
    //        TestPartitionKeyExtractorClass.class, ()-> new TestPartitionKeyExtractorClass());
    //this.applicationContext.registerBean("pkSelector",
    //        TestPartitionSelectorClass.class, ()-> new TestPartitionSelectorClass());
    //producerProperties.PartitionKeyExtractorName = "pkExtractor";
    //producerProperties.PartitionSelectorName = "pkSelector";
    //producerProperties.Extension.Prefix = "foo.";
    //producerProperties.Extension
    //        .DeliveryMode = MessageDeliveryMode.NON_PERSISTENT;
    //producerProperties.Extension.HeaderPatterns = new String[] { "foo" };
    //producerProperties
    //        .PartitionKeyExpression(spelExpressionParser.parseExpression = "'foo'");
    //producerProperties.setPartitionSelectorExpression(
    //        spelExpressionParser.parseExpression("0"));
    //producerProperties.PartitionCount = 1;
    //producerProperties.Extension.Transacted = true;
    //producerProperties.Extension
    //        .DelayExpression(spelExpressionParser.parseExpression = "42");
    //producerProperties.RequiredGroups = "prodPropsRequired";

    //BindingProperties producerBindingProperties = createProducerBindingProperties(
    //        producerProperties);
    //DirectChannel channel = createBindableChannel("output",
    //        producerBindingProperties);
    //producerBinding = binder.bindProducer("props.0", channel, producerProperties);

    //ConnectionFactory producerConnectionFactory = TestUtils.getPropertyValue(
    //        producerBinding, "lifecycle.amqpTemplate.connectionFactory",
    //        ConnectionFactory.class);

    //assertThat(this.rabbitAvailableRule.getResource())
    //        .isSameAs(producerConnectionFactory);

    //endpoint = extractEndpoint(producerBinding);
    //assertThat(getEndpointRouting(endpoint)).isEqualTo(
    //        "'props.0-' + headers['" + BinderHeaders.PARTITION_HEADER + "']");
    //assertThat(TestUtils
    //        .getPropertyValue(endpoint, "delayExpression", SpelExpression.class)
    //				.getExpressionString()).isEqualTo("42");
    //mode = TestUtils.getPropertyValue(endpoint, "defaultDeliveryMode",
    //        MessageDeliveryMode.class);
    //assertThat(mode).isEqualTo(MessageDeliveryMode.NON_PERSISTENT);
    //assertThat(TestUtils.getPropertyValue(endpoint, "amqpTemplate.transactional",
    //        Boolean.class)).isTrue();
    //verifyFooRequestProducer(endpoint);
    //channel.send(new GenericMessage<>("foo"));
    //org.springframework.amqp.core.Message received = new RabbitTemplate(
    //        this.rabbitAvailableRule.getResource())
    //        .receive("foo.props.0.prodPropsRequired-0", 10_000);
    //assertThat(received).isNotNull();
    //assertThat(received.getMessageProperties().getReceivedDelay()).isEqualTo(42);

    //producerBinding.unbind();
    //assertThat(endpoint.isRunning()).isFalse();
    //	}

    //	@Test
    //    public void testDurablePubSubWithAutoBindDLQ() throws Exception
    //{
    //    RabbitAdmin admin = new RabbitAdmin(this.rabbitAvailableRule.getResource());

    //RabbitTestBinder binder = getBinder();

    //ExtendedConsumerProperties<RabbitConsumerProperties> consumerProperties = createConsumerProperties();
    //consumerProperties.Extension.Prefix = TEST_PREFIX;
    //consumerProperties.Extension.AutoBindDlq = true;
    //consumerProperties.Extension.DurableSubscription = true;
    //consumerProperties.MaxAttempts = 1; // disable retry
    //DirectChannel moduleInputChannel = createBindableChannel("input",
    //        createConsumerBindingProperties(consumerProperties));
    //moduleInputChannel.BeanName = "durableTest";
    //moduleInputChannel.subscribe(new MessageHandler() {

    //            @Override

    //            public void handleMessage(Message<?> message) throws MessagingException
    //{
    //				throw new RuntimeException("foo");
    //			}

    //		});
    //Binding<MessageChannel> consumerBinding = binder.bindConsumer("durabletest.0",
    //        "tgroup", moduleInputChannel, consumerProperties);

    //RabbitTemplate template = new RabbitTemplate(
    //        this.rabbitAvailableRule.getResource());
    //template.convertAndSend(TEST_PREFIX + "durabletest.0", "", "foo");

    //int n = 0;
    //while (n++ < 100)
    //{
    //    Object deadLetter = template
    //            .receiveAndConvert(TEST_PREFIX + "durabletest.0.tgroup.dlq");
    //    if (deadLetter != null)
    //    {
    //        assertThat(deadLetter).isEqualTo("foo");
    //        break;
    //    }
    //    Thread.sleep(100);
    //}
    //assertThat(n).isLessThan(100);

    //consumerBinding.unbind();
    //assertThat(admin.getQueueProperties(TEST_PREFIX + "durabletest.0.tgroup.dlq"))
    //        .isNotNull();
    //	}

    //	@Test
    //    public void testNonDurablePubSubWithAutoBindDLQ() throws Exception
    //{
    //    RabbitAdmin admin = new RabbitAdmin(this.rabbitAvailableRule.getResource());

    //RabbitTestBinder binder = getBinder();
    //ExtendedConsumerProperties<RabbitConsumerProperties> consumerProperties = createConsumerProperties();
    //consumerProperties.Extension.Prefix = TEST_PREFIX;
    //consumerProperties.Extension.AutoBindDlq = true;
    //consumerProperties.Extension.DurableSubscription = false;
    //consumerProperties.MaxAttempts = 1; // disable retry
    //BindingProperties bindingProperties = createConsumerBindingProperties(
    //        consumerProperties);
    //DirectChannel moduleInputChannel = createBindableChannel("input",
    //        bindingProperties);
    //moduleInputChannel.BeanName = "nondurabletest";
    //moduleInputChannel.subscribe(new MessageHandler() {

    //            @Override

    //            public void handleMessage(Message<?> message) throws MessagingException
    //{
    //				throw new RuntimeException("foo");
    //			}

    //		});
    //Binding<MessageChannel> consumerBinding = binder.bindConsumer("nondurabletest.0",
    //        "tgroup", moduleInputChannel, consumerProperties);

    //consumerBinding.unbind();
    //assertThat(admin.getQueueProperties(TEST_PREFIX + "nondurabletest.0.dlq"))
    //        .isNull();
    //	}

    //	@Test
    //    public void testAutoBindDLQ() throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    ExtendedConsumerProperties<RabbitConsumerProperties> consumerProperties = createConsumerProperties();
    //    consumerProperties.Extension.Prefix = TEST_PREFIX;
    //    consumerProperties.Extension.AutoBindDlq = true;
    //    consumerProperties.MaxAttempts = 1; // disable retry
    //    consumerProperties.Extension.DurableSubscription = true;
    //    BindingProperties bindingProperties = createConsumerBindingProperties(
    //				consumerProperties);
    //    DirectChannel moduleInputChannel = createBindableChannel("input",
    //				bindingProperties);
    //    moduleInputChannel.BeanName = "dlqTest";
    //    moduleInputChannel.subscribe(new MessageHandler() {

    //            @Override

    //            public void handleMessage(Message<?> message) throws MessagingException
    //{
    //				throw new RuntimeException("foo");
    //			}

    //		});
    //consumerProperties.Multiplex = true;
    //Binding<MessageChannel> consumerBinding = binder.bindConsumer("dlqtest,dlqtest2",
    //        "default", moduleInputChannel, consumerProperties);
    //AbstractMessageListenerContainer container = TestUtils.getPropertyValue(
    //        consumerBinding, "lifecycle.messageListenerContainer",
    //        AbstractMessageListenerContainer.class);
    //assertThat(container.getQueueNames().length).isEqualTo(2);

    //RabbitTemplate template = new RabbitTemplate(
    //        this.rabbitAvailableRule.getResource());
    //template.convertAndSend("", TEST_PREFIX + "dlqtest.default", "foo");

    //int n = 0;
    //while (n++ < 100)
    //{
    //    Object deadLetter = template
    //            .receiveAndConvert(TEST_PREFIX + "dlqtest.default.dlq");
    //    if (deadLetter != null)
    //    {
    //        assertThat(deadLetter).isEqualTo("foo");
    //        break;
    //    }
    //    Thread.sleep(100);
    //}
    //assertThat(n).isLessThan(100);

    //template.convertAndSend("", TEST_PREFIX + "dlqtest2.default", "bar");

    //n = 0;
    //while (n++ < 100)
    //{
    //    Object deadLetter = template
    //            .receiveAndConvert(TEST_PREFIX + "dlqtest2.default.dlq");
    //    if (deadLetter != null)
    //    {
    //        assertThat(deadLetter).isEqualTo("bar");
    //        break;
    //    }
    //    Thread.sleep(100);
    //}
    //assertThat(n).isLessThan(100);

    //consumerBinding.unbind();

    //ApplicationContext context = TestUtils.getPropertyValue(binder,
    //        "binder.provisioningProvider.autoDeclareContext",
    //        ApplicationContext.class);
    //assertThat(context.containsBean(TEST_PREFIX + "dlqtest.default.binding"))
    //        .isFalse();
    //assertThat(context.containsBean(TEST_PREFIX + "dlqtest.default")).isFalse();
    //assertThat(context.containsBean(TEST_PREFIX + "dlqtest.default.dlq.binding"))
    //        .isFalse();
    //assertThat(context.containsBean(TEST_PREFIX + "dlqtest.default.dlq")).isFalse();
    //	}

    //	@Test
    //    public void testAutoBindDLQManualAcks() throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    ExtendedConsumerProperties<RabbitConsumerProperties> consumerProperties = createConsumerProperties();
    //    consumerProperties.Extension.Prefix = TEST_PREFIX;
    //    consumerProperties.Extension.AutoBindDlq = true;
    //    consumerProperties.MaxAttempts = 2;
    //    consumerProperties.Extension.DurableSubscription = true;
    //    consumerProperties.Extension.AcknowledgeMode = AcknowledgeMode.MANUAL;
    //    BindingProperties bindingProperties = createConsumerBindingProperties(
    //				consumerProperties);
    //    DirectChannel moduleInputChannel = createBindableChannel("input",
    //				bindingProperties);
    //    moduleInputChannel.BeanName = "dlqTestManual";
    //    Client client = new Client("http://guest:guest@localhost:15672/api");
    //moduleInputChannel.subscribe(new MessageHandler() {

    //            @Override

    //            public void handleMessage(Message<?> message) throws MessagingException
    //{
    //    // Wait until the unacked state is reflected in the admin
    //    QueueInfo info = client.getQueue("/", TEST_PREFIX + "dlqTestManual.default");
    //				int n = 0;
    //				while (n++ < 100 && info.getMessagesUnacknowledged() < 1L) {
    //        try
    //        {
    //            Thread.sleep(100);
    //        }
    //        catch (InterruptedException e)
    //        {
    //            Thread.currentThread().interrupt();
    //        }
    //        info = client.getQueue("/", TEST_PREFIX + "dlqTestManual.default");
    //    }
    //				throw new RuntimeException("foo");
    //			}

    //		});
    //Binding<MessageChannel> consumerBinding = binder.bindConsumer("dlqTestManual",
    //        "default", moduleInputChannel, consumerProperties);

    //RabbitTemplate template = new RabbitTemplate(
    //        this.rabbitAvailableRule.getResource());
    //template.convertAndSend("", TEST_PREFIX + "dlqTestManual.default", "foo");

    //int n = 0;
    //while (n++ < 100)
    //{
    //    Object deadLetter = template
    //            .receiveAndConvert(TEST_PREFIX + "dlqTestManual.default.dlq");
    //    if (deadLetter != null)
    //    {
    //        assertThat(deadLetter).isEqualTo("foo");
    //        break;
    //    }
    //    Thread.sleep(100);
    //}
    //assertThat(n).isLessThan(100);

    //n = 0;
    //QueueInfo info = client.getQueue("/", TEST_PREFIX + "dlqTestManual.default");
    //while (n++ < 100 && info.getMessagesUnacknowledged() > 0L)
    //{
    //    Thread.sleep(100);
    //    info = client.getQueue("/", TEST_PREFIX + "dlqTestManual.default");
    //}
    //assertThat(info.getMessagesUnacknowledged()).isEqualTo(0L);

    //consumerBinding.unbind();

    //ApplicationContext context = TestUtils.getPropertyValue(binder,
    //        "binder.provisioningProvider.autoDeclareContext",
    //        ApplicationContext.class);
    //assertThat(context.containsBean(TEST_PREFIX + "dlqTestManual.default.binding"))
    //        .isFalse();
    //assertThat(context.containsBean(TEST_PREFIX + "dlqTestManual.default")).isFalse();
    //assertThat(context.containsBean(TEST_PREFIX + "dlqTestManual.default.dlq.binding"))
    //        .isFalse();
    //assertThat(context.containsBean(TEST_PREFIX + "dlqTestManual.default.dlq")).isFalse();
    //	}

    //	@Test
    //    public void testAutoBindDLQPartionedConsumerFirst() throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    ExtendedConsumerProperties<RabbitConsumerProperties> properties = createConsumerProperties();
    //    properties.Extension.Prefix = "bindertest.";
    //    properties.Extension.AutoBindDlq = true;
    //    properties.MaxAttempts = 1; // disable retry
    //    properties.Partitioned = true;
    //    properties.InstanceIndex = 0;
    //    DirectChannel input0 = createBindableChannel("input",
    //				createConsumerBindingProperties(properties));
    //    input0.BeanName = "test.input0DLQ";
    //    Binding<MessageChannel> input0Binding = binder.bindConsumer("partDLQ.0",
    //				"dlqPartGrp", input0, properties);
    //    Binding<MessageChannel> defaultConsumerBinding1 = binder.bindConsumer("partDLQ.0",
    //				"default", new QueueChannel(), properties);
    //properties.InstanceIndex = 1;
    //DirectChannel input1 = createBindableChannel("input1",
    //        createConsumerBindingProperties(properties));
    //input1.BeanName = "test.input1DLQ";
    //Binding<MessageChannel> input1Binding = binder.bindConsumer("partDLQ.0",
    //        "dlqPartGrp", input1, properties);
    //Binding<MessageChannel> defaultConsumerBinding2 = binder.bindConsumer("partDLQ.0",
    //        "default", new QueueChannel(), properties);

    //ExtendedProducerProperties<RabbitProducerProperties> producerProperties = createProducerProperties();
    //producerProperties.Extension.Prefix = "bindertest.";
    //this.applicationContext.registerBean("pkExtractor", PartitionTestSupport.class, ()-> new PartitionTestSupport());
    //this.applicationContext.registerBean("pkSelector", PartitionTestSupport.class, ()-> new PartitionTestSupport());
    //producerProperties.Extension.AutoBindDlq = true;
    //producerProperties.PartitionKeyExtractorName = "pkExtractor";
    //producerProperties.PartitionSelectorName = "pkSelector";
    //producerProperties.PartitionCount = 2;
    //BindingProperties bindingProperties = createProducerBindingProperties(
    //        producerProperties);
    //DirectChannel output = createBindableChannel("output", bindingProperties);
    //output.BeanName = "test.output";
    //Binding<MessageChannel> outputBinding = binder.bindProducer("partDLQ.0", output,
    //        producerProperties);

    //final CountDownLatch latch0 = new CountDownLatch(1);
    //input0.subscribe(new MessageHandler() {

    //            @Override

    //            public void handleMessage(Message<?> message) throws MessagingException
    //{
    //				if (latch0.getCount() <= 0) {
    //        throw new RuntimeException("dlq");
    //    }
    //    latch0.countDown();
    //}

    //		});

    //final CountDownLatch latch1 = new CountDownLatch(1);
    //input1.subscribe(new MessageHandler() {

    //            @Override

    //            public void handleMessage(Message<?> message) throws MessagingException
    //{
    //				if (latch1.getCount() <= 0) {
    //        throw new RuntimeException("dlq");
    //    }
    //    latch1.countDown();
    //}

    //		});

    //output.send(new GenericMessage<>(1));
    //assertThat(latch1.await(10, TimeUnit.SECONDS)).isTrue();

    //output.send(new GenericMessage<>(0));
    //assertThat(latch0.await(10, TimeUnit.SECONDS)).isTrue();

    //output.send(new GenericMessage<>(1));

    //RabbitTemplate template = new RabbitTemplate(
    //        this.rabbitAvailableRule.getResource());
    //template.ReceiveTimeout = 10000;

    //String streamDLQName = "bindertest.partDLQ.0.dlqPartGrp.dlq";

    //org.springframework.amqp.core.Message received = template.receive(streamDLQName);
    //assertThat(received).isNotNull();
    //assertThat(received.getMessageProperties().getReceivedRoutingKey())
    //        .isEqualTo("bindertest.partDLQ.0.dlqPartGrp-1");
    //assertThat(received.getMessageProperties().getHeaders())
    //        .doesNotContainKey(BinderHeaders.PARTITION_HEADER);

    //output.send(new GenericMessage<>(0));
    //received = template.receive(streamDLQName);
    //assertThat(received).isNotNull();
    //assertThat(received.getMessageProperties().getReceivedRoutingKey())
    //        .isEqualTo("bindertest.partDLQ.0.dlqPartGrp-0");
    //assertThat(received.getMessageProperties().getHeaders())
    //        .doesNotContainKey(BinderHeaders.PARTITION_HEADER);

    //input0Binding.unbind();
    //input1Binding.unbind();
    //defaultConsumerBinding1.unbind();
    //defaultConsumerBinding2.unbind();
    //outputBinding.unbind();
    //	}

    //	@Test
    //    public void testAutoBindDLQPartitionedConsumerFirstWithRepublishNoRetry()

    //            throws Exception
    //{
    //    testAutoBindDLQPartionedConsumerFirstWithRepublishGuts(false);
    //}

    //@Test
    //    public void testAutoBindDLQPartitionedConsumerFirstWithRepublishWithRetry()

    //            throws Exception
    //{
    //    testAutoBindDLQPartionedConsumerFirstWithRepublishGuts(true);
    //}

    //private void testAutoBindDLQPartionedConsumerFirstWithRepublishGuts(
    //        final boolean withRetry) throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    ExtendedConsumerProperties<RabbitConsumerProperties> properties = createConsumerProperties();
    //    properties.Extension.Prefix = "bindertest.";
    //    properties.Extension.AutoBindDlq = true;
    //    properties.Extension.RepublishToDlq = true;
    //    properties.Extension
    //				.RepublishDeliveyMode = MessageDeliveryMode.NON_PERSISTENT;
    //    properties.MaxAttempts = withRetry ? 2 : 1;
    //    properties.Partitioned = true;
    //    properties.InstanceIndex = 0;
    //    DirectChannel input0 = createBindableChannel("input",
    //				createConsumerBindingProperties(properties));
    //    input0.BeanName = "test.input0DLQ";
    //    Binding<MessageChannel> input0Binding = binder.bindConsumer("partPubDLQ.0",
    //				"dlqPartGrp", input0, properties);
    //    Binding<MessageChannel> defaultConsumerBinding1 = binder
    //				.bindConsumer("partPubDLQ.0", "default", new QueueChannel(), properties);
    //properties.InstanceIndex = 1;
    //DirectChannel input1 = createBindableChannel("input1",
    //        createConsumerBindingProperties(properties));
    //input1.BeanName = "test.input1DLQ";
    //Binding<MessageChannel> input1Binding = binder.bindConsumer("partPubDLQ.0",
    //        "dlqPartGrp", input1, properties);
    //Binding<MessageChannel> defaultConsumerBinding2 = binder
    //        .bindConsumer("partPubDLQ.0", "default", new QueueChannel(), properties);

    //ExtendedProducerProperties<RabbitProducerProperties> producerProperties = createProducerProperties();
    //producerProperties.Extension.Prefix = "bindertest.";
    //producerProperties.Extension.AutoBindDlq = true;
    //this.applicationContext.registerBean("pkExtractor", PartitionTestSupport.class, ()-> new PartitionTestSupport());
    //this.applicationContext.registerBean("pkSelector", PartitionTestSupport.class, ()-> new PartitionTestSupport());
    //producerProperties.PartitionKeyExtractorName = "pkExtractor";
    //producerProperties.PartitionSelectorName = "pkSelector";
    //producerProperties.PartitionCount = 2;
    //BindingProperties bindingProperties = createProducerBindingProperties(
    //        producerProperties);
    //DirectChannel output = createBindableChannel("output", bindingProperties);
    //output.BeanName = "test.output";
    //Binding<MessageChannel> outputBinding = binder.bindProducer("partPubDLQ.0",
    //        output, producerProperties);

    //final CountDownLatch latch0 = new CountDownLatch(1);
    //input0.subscribe(new MessageHandler() {

    //            @Override

    //            public void handleMessage(Message<?> message) throws MessagingException
    //{
    //				if (latch0.getCount() <= 0) {
    //        throw new RuntimeException("dlq");
    //    }
    //    latch0.countDown();
    //}

    //		});

    //final CountDownLatch latch1 = new CountDownLatch(1);
    //input1.subscribe(new MessageHandler() {

    //            @Override

    //            public void handleMessage(Message<?> message) throws MessagingException
    //{
    //				if (latch1.getCount() <= 0) {
    //        throw new RuntimeException("dlq");
    //    }
    //    latch1.countDown();
    //}

    //		});

    //ApplicationContext context = TestUtils.getPropertyValue(binder.getBinder(),
    //        "applicationContext", ApplicationContext.class);
    //SubscribableChannel boundErrorChannel = context.getBean(
    //        "bindertest.partPubDLQ.0.dlqPartGrp-0.errors", SubscribableChannel.class);
    //SubscribableChannel globalErrorChannel = context.getBean("errorChannel",
    //        SubscribableChannel.class);
    //final AtomicReference<Message<?>> boundErrorChannelMessage = new AtomicReference<>();
    //final AtomicReference<Message<?>> globalErrorChannelMessage = new AtomicReference<>();
    //final AtomicBoolean hasRecovererInCallStack = new AtomicBoolean(!withRetry);
    //boundErrorChannel.subscribe(new MessageHandler() {

    //            @Override

    //            public void handleMessage(Message<?> message) throws MessagingException
    //{
    //    boundErrorChannelMessage.set(message);
    //    String stackTrace = Arrays
    //            .toString(new RuntimeException().getStackTrace());
    //    hasRecovererInCallStack
    //            .(stackTrace.contains = "ErrorMessageSendingRecoverer");
    //    }

    //});
    //globalErrorChannel.subscribe(new MessageHandler() {

    //            @Override

    //            public void handleMessage(Message<?> message) throws MessagingException
    //{
    //    globalErrorChannelMessage.set(message);
    //    }

    //});

    //output.send(new GenericMessage<>(1));
    //assertThat(latch1.await(10, TimeUnit.SECONDS)).isTrue();

    //output.send(new GenericMessage<>(0));
    //assertThat(latch0.await(10, TimeUnit.SECONDS)).isTrue();

    //output.send(new GenericMessage<>(1));

    //RabbitTemplate template = new RabbitTemplate(
    //        this.rabbitAvailableRule.getResource());
    //template.ReceiveTimeout = 10000;

    //String streamDLQName = "bindertest.partPubDLQ.0.dlqPartGrp.dlq";

    //org.springframework.amqp.core.Message received = template.receive(streamDLQName);
    //assertThat(received).isNotNull();
    //assertThat(
    //        received.getMessageProperties().getHeaders().get("x-original-routingKey"))
    //        .isEqualTo("partPubDLQ.0-1");
    //assertThat(received.getMessageProperties().getHeaders())
    //        .doesNotContainKey(BinderHeaders.PARTITION_HEADER);
    //assertThat(received.getMessageProperties().getReceivedDeliveryMode())
    //        .isEqualTo(MessageDeliveryMode.NON_PERSISTENT);

    //output.send(new GenericMessage<>(0));
    //received = template.receive(streamDLQName);
    //assertThat(received).isNotNull();
    //assertThat(
    //        received.getMessageProperties().getHeaders().get("x-original-routingKey"))
    //        .isEqualTo("partPubDLQ.0-0");
    //assertThat(received.getMessageProperties().getHeaders())
    //        .doesNotContainKey(BinderHeaders.PARTITION_HEADER);

    //// verify we got a message on the dedicated error channel and the global (via
    //// bridge)
    //assertThat(boundErrorChannelMessage.get()).isNotNull();
    //assertThat(globalErrorChannelMessage.get()).isNotNull();
    //assertThat(hasRecovererInCallStack.get()).isEqualTo(withRetry);

    //input0Binding.unbind();
    //input1Binding.unbind();
    //defaultConsumerBinding1.unbind();
    //defaultConsumerBinding2.unbind();
    //outputBinding.unbind();
    //	}

    //	@Test
    //    public void testAutoBindDLQPartitionedProducerFirst() throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    ExtendedProducerProperties<RabbitProducerProperties> properties = createProducerProperties();

    //    properties.Extension.Prefix = "bindertest.";
    //    properties.Extension.AutoBindDlq = true;
    //    properties.RequiredGroups = "dlqPartGrp";
    //		this.applicationContext.registerBean("pkExtractor", PartitionTestSupport.class, ()-> new PartitionTestSupport());
    //properties.PartitionKeyExtractorName = "pkExtractor";
    //properties.PartitionSelectorName = "pkExtractor";
    //properties.PartitionCount = 2;
    //DirectChannel output = createBindableChannel("output",
    //        createProducerBindingProperties(properties));
    //output.BeanName = "test.output";
    //Binding<MessageChannel> outputBinding = binder.bindProducer("partDLQ.1", output,
    //        properties);

    //ExtendedConsumerProperties<RabbitConsumerProperties> consumerProperties = createConsumerProperties();
    //consumerProperties.Extension.Prefix = "bindertest.";
    //consumerProperties.Extension.AutoBindDlq = true;
    //consumerProperties.MaxAttempts = 1; // disable retry
    //consumerProperties.Partitioned = true;
    //consumerProperties.InstanceIndex = 0;
    //DirectChannel input0 = createBindableChannel("input",
    //        createConsumerBindingProperties(consumerProperties));
    //input0.BeanName = "test.input0DLQ";
    //Binding<MessageChannel> input0Binding = binder.bindConsumer("partDLQ.1",
    //        "dlqPartGrp", input0, consumerProperties);
    //Binding<MessageChannel> defaultConsumerBinding1 = binder.bindConsumer("partDLQ.1",
    //        "defaultConsumer", new QueueChannel(), consumerProperties);
    //consumerProperties.InstanceIndex = 1;
    //DirectChannel input1 = createBindableChannel("input1",
    //        createConsumerBindingProperties(consumerProperties));
    //input1.BeanName = "test.input1DLQ";
    //Binding<MessageChannel> input1Binding = binder.bindConsumer("partDLQ.1",
    //        "dlqPartGrp", input1, consumerProperties);
    //Binding<MessageChannel> defaultConsumerBinding2 = binder.bindConsumer("partDLQ.1",
    //        "defaultConsumer", new QueueChannel(), consumerProperties);

    //final CountDownLatch latch0 = new CountDownLatch(1);
    //input0.subscribe(new MessageHandler() {

    //            @Override

    //            public void handleMessage(Message<?> message) throws MessagingException
    //{
    //				if (latch0.getCount() <= 0) {
    //        throw new RuntimeException("dlq");
    //    }
    //    latch0.countDown();
    //}

    //		});

    //final CountDownLatch latch1 = new CountDownLatch(1);
    //input1.subscribe(new MessageHandler() {

    //            @Override

    //            public void handleMessage(Message<?> message) throws MessagingException
    //{
    //				if (latch1.getCount() <= 0) {
    //        throw new RuntimeException("dlq");
    //    }
    //    latch1.countDown();
    //}

    //		});

    //output.send(new GenericMessage<Integer>(1));
    //assertThat(latch1.await(10, TimeUnit.SECONDS)).isTrue();

    //output.send(new GenericMessage<Integer>(0));
    //assertThat(latch0.await(10, TimeUnit.SECONDS)).isTrue();

    //output.send(new GenericMessage<Integer>(1));

    //RabbitTemplate template = new RabbitTemplate(
    //        this.rabbitAvailableRule.getResource());
    //template.ReceiveTimeout = 10000;

    //String streamDLQName = "bindertest.partDLQ.1.dlqPartGrp.dlq";

    //org.springframework.amqp.core.Message received = template.receive(streamDLQName);
    //assertThat(received).isNotNull();
    //assertThat(received.getMessageProperties().getReceivedRoutingKey())
    //        .isEqualTo("bindertest.partDLQ.1.dlqPartGrp-1");
    //assertThat(received.getMessageProperties().getHeaders())
    //        .doesNotContainKey(BinderHeaders.PARTITION_HEADER);
    //assertThat(received.getMessageProperties().getReceivedDeliveryMode())
    //        .isEqualTo(MessageDeliveryMode.PERSISTENT);

    //output.send(new GenericMessage<Integer>(0));
    //received = template.receive(streamDLQName);
    //assertThat(received).isNotNull();
    //assertThat(received.getMessageProperties().getReceivedRoutingKey())
    //        .isEqualTo("bindertest.partDLQ.1.dlqPartGrp-0");
    //assertThat(received.getMessageProperties().getHeaders())
    //        .doesNotContainKey(BinderHeaders.PARTITION_HEADER);

    //input0Binding.unbind();
    //input1Binding.unbind();
    //defaultConsumerBinding1.unbind();
    //defaultConsumerBinding2.unbind();
    //outputBinding.unbind();
    //	}

    //	@Test
    //    public void testAutoBindDLQwithRepublish() throws Exception
    //{
    //		this.maxStackTraceSize = RabbitUtils
    //				.getMaxFrame(rabbitAvailableRule.getResource()) - 20_000;
    //    assertThat(this.maxStackTraceSize).isGreaterThan(0);

    //    RabbitTestBinder binder = getBinder();
    //    ExtendedConsumerProperties<RabbitConsumerProperties> consumerProperties = createConsumerProperties();
    //    consumerProperties.Extension.Prefix = TEST_PREFIX;
    //    consumerProperties.Extension.AutoBindDlq = true;
    //    consumerProperties.Extension.RepublishToDlq = true;
    //    consumerProperties.MaxAttempts = 1; // disable retry
    //    consumerProperties.Extension.DurableSubscription = true;
    //    DirectChannel moduleInputChannel = createBindableChannel("input",
    //				createConsumerBindingProperties(consumerProperties));
    //    moduleInputChannel.BeanName = "dlqPubTest";
    //    RuntimeException exception = bigCause(

    //                new RuntimeException(BIG_EXCEPTION_MESSAGE));
    //assertThat(getStackTraceAsString(exception).length())
    //        .isGreaterThan(this.maxStackTraceSize);
    //AtomicBoolean dontRepublish = new AtomicBoolean();
    //moduleInputChannel.subscribe(new MessageHandler() {

    //            @Override

    //            public void handleMessage(Message<?> message) throws MessagingException
    //{
    //				if (dontRepublish.get()) {
    //        throw new ImmediateAcknowledgeAmqpException("testDontRepublish");
    //    }
    //    throw exception;
    //    }

    //});
    //consumerProperties.Multiplex = true;
    //Binding<MessageChannel> consumerBinding = binder.bindConsumer(
    //        "foo.dlqpubtest,foo.dlqpubtest2", "foo", moduleInputChannel,
    //        consumerProperties);

    //RabbitTemplate template = new RabbitTemplate(
    //        this.rabbitAvailableRule.getResource());
    //template.convertAndSend("", TEST_PREFIX + "foo.dlqpubtest.foo", "foo");

    //template.ReceiveTimeout = 10_000;
    //org.springframework.amqp.core.Message deadLetter = template
    //        .receive(TEST_PREFIX + "foo.dlqpubtest.foo.dlq");
    //assertThat(deadLetter).isNotNull();
    //assertThat(new String(deadLetter.getBody())).isEqualTo("foo");
    //assertThat(deadLetter.getMessageProperties().getHeaders())
    //        .containsKey((RepublishMessageRecoverer.X_EXCEPTION_STACKTRACE));
    //assertThat(((LongString)deadLetter.getMessageProperties().getHeaders()
    //        .get(RepublishMessageRecoverer.X_EXCEPTION_STACKTRACE)).length())
    //        .isEqualTo(this.maxStackTraceSize);

    //template.convertAndSend("", TEST_PREFIX + "foo.dlqpubtest2.foo", "bar");

    //deadLetter = template.receive(TEST_PREFIX + "foo.dlqpubtest2.foo.dlq");
    //assertThat(deadLetter).isNotNull();
    //assertThat(new String(deadLetter.getBody())).isEqualTo("bar");
    //assertThat(deadLetter.getMessageProperties().getHeaders())
    //        .containsKey(("x-exception-stacktrace"));

    //dontRepublish.set(true);
    //template.convertAndSend("", TEST_PREFIX + "foo.dlqpubtest2.foo", "baz");
    //template.ReceiveTimeout = 500;
    //assertThat(template.receive(TEST_PREFIX + "foo.dlqpubtest2.foo.dlq")).isNull();

    //consumerBinding.unbind();
    //	}

    //	@SuppressWarnings("unchecked")

    //    @Test
    //    public void testBatchingAndCompression() throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    ExtendedProducerProperties<RabbitProducerProperties> producerProperties = createProducerProperties();
    //    producerProperties.Extension
    //				.DeliveryMode = MessageDeliveryMode.NON_PERSISTENT;
    //    producerProperties.Extension.BatchingEnabled = true;
    //    producerProperties.Extension.BatchSize = 2;
    //    producerProperties.Extension.BatchBufferLimit = 100000;
    //    producerProperties.Extension.BatchTimeout = 30000;
    //    producerProperties.Extension.Compress = true;
    //    producerProperties.RequiredGroups = "default";

    //    DirectChannel output = createBindableChannel("output",
    //				createProducerBindingProperties(producerProperties));
    //    output.BeanName = "batchingProducer";
    //    Binding<MessageChannel> producerBinding = binder.bindProducer("batching.0",
    //				output, producerProperties);

    //    Log logger = spy(TestUtils.getPropertyValue(binder,
    //				"binder.compressingPostProcessor.logger", Log.class));
    //new DirectFieldAccessor(
    //        TestUtils.getPropertyValue(binder, "binder.compressingPostProcessor"))
    //        .PropertyValue = "logger", logger;
    //when(logger.isTraceEnabled()).thenReturn(true);

    //assertThat(TestUtils.getPropertyValue(binder,
    //        "binder.compressingPostProcessor.level")).isEqualTo(Deflater.BEST_SPEED);

    //output.send(new GenericMessage<>("foo".getBytes()));
    //output.send(new GenericMessage<>("bar".getBytes()));

    //Object out = spyOn("batching.0.default").receive(false);
    //assertThat(out).isInstanceOf(byte[].class);
    //assertThat(new String((byte[]) out))
    //        .isEqualTo("\u0000\u0000\u0000\u0003foo\u0000\u0000\u0000\u0003bar");

    //ArgumentCaptor<Object> captor = ArgumentCaptor.forClass(Object.class);
    //verify(logger).trace(captor.capture());
    //assertThat(captor.getValue().toString()).contains(("Compressed 14 to "));

    //QueueChannel input = new QueueChannel();
    //input.BeanName = "batchingConsumer";
    //Binding<MessageChannel> consumerBinding = binder.bindConsumer("batching.0",
    //        "test", input, createConsumerProperties());

    //output.send(new GenericMessage<>("foo".getBytes()));
    //output.send(new GenericMessage<>("bar".getBytes()));

    //Message<byte[]> in = (Message<byte[]>)input.receive(10000);
    //assertThat(in).isNotNull();
    //assertThat(new String(in.getPayload())).isEqualTo("foo");
    //		in = (Message<byte[]>)input.receive(10000);
    //assertThat(in).isNotNull();
    //assertThat(new String(in.getPayload())).isEqualTo("bar");
    //assertThat(in.getHeaders().get(AmqpHeaders.DELIVERY_MODE)).isNull();

    //producerBinding.unbind();
    //consumerBinding.unbind();
    //	}

    //	@SuppressWarnings("unchecked")

    //    @Test
    //    public void testProducerBatching() throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    ExtendedProducerProperties<RabbitProducerProperties> producerProperties = createProducerProperties();
    //    producerProperties.Extension
    //				.DeliveryMode = MessageDeliveryMode.NON_PERSISTENT;
    //    producerProperties.Extension.BatchingEnabled = true;
    //    producerProperties.Extension.BatchSize = 2;
    //    producerProperties.Extension.BatchBufferLimit = 100000;
    //    producerProperties.Extension.BatchTimeout = 30000;
    //    producerProperties.Extension.Compress = true;

    //    DirectChannel output = createBindableChannel("output",
    //				createProducerBindingProperties(producerProperties));
    //    output.BeanName = "producerBatchingProducer";
    //    Binding<MessageChannel> producerBinding = binder.bindProducer("p.batching.0",
    //				output, producerProperties);

    //    QueueChannel input = new QueueChannel();
    //input.BeanName = "producerBatchingConsumer";
    //ExtendedConsumerProperties<RabbitConsumerProperties> consumerProperties = createConsumerProperties();
    //consumerProperties.BatchMode = true;
    //consumerProperties.Extension.BatchSize = 2;
    //Binding<MessageChannel> consumerBinding = binder.bindConsumer("p.batching.0",
    //        "producerBatching", input, consumerProperties);
    //output.send(new GenericMessage<>("foo".getBytes()));
    //output.send(new GenericMessage<>("bar".getBytes()));


    //Message <?> in = input.receive(10000);
    //assertThat(in).isNotNull();
    //assertThat(in.getPayload()).isInstanceOf(List.class);
    //List<byte[]> payload = (List<byte[]>) in.getPayload();
    //assertThat(payload).hasSize(2);
    //assertThat(payload.get(0)).isEqualTo("foo".getBytes());
    //assertThat(payload.get(1)).isEqualTo("bar".getBytes());

    //producerBinding.unbind();
    //consumerBinding.unbind();
    //	}

    //	@SuppressWarnings("unchecked")

    //    @Test
    //    public void testConsumerBatching() throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    ExtendedProducerProperties<RabbitProducerProperties> producerProperties = createProducerProperties();
    //    producerProperties.Extension
    //				.DeliveryMode = MessageDeliveryMode.NON_PERSISTENT;

    //    DirectChannel output = createBindableChannel("output",
    //				createProducerBindingProperties(producerProperties));
    //    output.BeanName = "consumerBatching.Producer";
    //    Binding<MessageChannel> producerBinding = binder.bindProducer("c.batching.0",
    //				output, producerProperties);

    //    QueueChannel input = new QueueChannel();
    //input.BeanName = "batchingConsumer";
    //ExtendedConsumerProperties<RabbitConsumerProperties> consumerProperties = createConsumerProperties();
    //consumerProperties.BatchMode = true;
    //consumerProperties.Extension.BatchSize = 2;
    //consumerProperties.Extension.EnableBatching = true;
    //Binding<MessageChannel> consumerBinding = binder.bindConsumer("c.batching.0",
    //        "consumerBatching", input, consumerProperties);
    //output.send(new GenericMessage<>("foo".getBytes()));
    //output.send(new GenericMessage<>("bar".getBytes()));


    //Message <?> in = input.receive(10000);
    //assertThat(in).isNotNull();
    //assertThat(in.getPayload()).isInstanceOf(List.class);
    //List<byte[]> payload = (List<byte[]>) in.getPayload();
    //assertThat(payload).hasSize(2);
    //assertThat(payload.get(0)).isEqualTo("foo".getBytes());
    //assertThat(payload.get(1)).isEqualTo("bar".getBytes());

    //producerBinding.unbind();
    //consumerBinding.unbind();
    //	}

    //	@SuppressWarnings("unchecked")

    //    @Test
    //    public void testInternalHeadersNotPropagated() throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    ExtendedProducerProperties<RabbitProducerProperties> producerProperties = createProducerProperties();
    //    producerProperties.Extension
    //				.DeliveryMode = MessageDeliveryMode.NON_PERSISTENT;

    //    DirectChannel output = createBindableChannel("output",
    //				createProducerBindingProperties(producerProperties));
    //    output.BeanName = "propagate.out";
    //    Binding<MessageChannel> producerBinding = binder.bindProducer("propagate.1",
    //				output, producerProperties);

    //    QueueChannel input = new QueueChannel();
    //input.BeanName = "propagate.in";
    //ExtendedConsumerProperties<RabbitConsumerProperties> consumerProperties = createConsumerProperties();
    //Binding<MessageChannel> consumerBinding = binder.bindConsumer("propagate.0",
    //        "propagate", input, consumerProperties);
    //RabbitAdmin admin = new RabbitAdmin(rabbitAvailableRule.getResource());
    //admin.declareQueue(new Queue("propagate"));
    //admin.declareBinding(new org.springframework.amqp.core.Binding("propagate", DestinationType.QUEUE,
    //        "propagate.1", "#", null));
    //RabbitTemplate template = new RabbitTemplate(this.rabbitAvailableRule.getResource());
    //template.convertAndSend("propagate.0.propagate", "foo");
    //output.send(input.receive(10_000));
    //org.springframework.amqp.core.Message received = template.receive("propagate", 10_000);
    //assertThat(received).isNotNull();
    //assertThat(received.getBody()).isEqualTo("foo".getBytes());
    //Object header = received.getMessageProperties().getHeader(IntegrationMessageHeaderAccessor.SOURCE_DATA);
    //assertThat(header).isNull();
    //header = received.getMessageProperties().getHeader(IntegrationMessageHeaderAccessor.DELIVERY_ATTEMPT);
    //assertThat(header).isNull();

    //producerBinding.unbind();
    //consumerBinding.unbind();
    //admin.deleteQueue("propagate");
    //	}

    //	/*
    //	 * Test late binding due to broker down; queues with and without DLQs, and partitioned
    //	 * queues.
    //	 */
    //	@Test
    //    public void testLateBinding() throws Exception
    //{
    //    RabbitTestSupport.RabbitProxy proxy = new RabbitTestSupport.RabbitProxy();
    //CachingConnectionFactory cf = new CachingConnectionFactory("localhost",
    //        proxy.getPort());

    //RabbitMessageChannelBinder rabbitBinder = new RabbitMessageChannelBinder(cf,
    //        new RabbitProperties(), new RabbitExchangeQueueProvisioner(cf));
    //RabbitTestBinder binder = new RabbitTestBinder(cf, rabbitBinder);

    //ExtendedProducerProperties<RabbitProducerProperties> producerProperties = createProducerProperties();
    //producerProperties.Extension.Prefix = "latebinder.";
    //producerProperties.Extension.AutoBindDlq = true;
    //producerProperties.Extension.Transacted = true;

    //MessageChannel moduleOutputChannel = createBindableChannel("output",
    //        createProducerBindingProperties(producerProperties));
    //Binding<MessageChannel> late0ProducerBinding = binder.bindProducer("late.0",
    //        moduleOutputChannel, producerProperties);

    //QueueChannel moduleInputChannel = new QueueChannel();
    //ExtendedConsumerProperties<RabbitConsumerProperties> rabbitConsumerProperties = createConsumerProperties();
    //rabbitConsumerProperties.Extension.Prefix = "latebinder.";
    //Binding<MessageChannel> late0ConsumerBinding = binder.bindConsumer("late.0",
    //        "test", moduleInputChannel, rabbitConsumerProperties);

    //producerProperties.setPartitionKeyExpression(
    //        spelExpressionParser.parseExpression("payload.equals('0') ? 0 : 1"));
    //producerProperties.setPartitionSelectorExpression(
    //        spelExpressionParser.parseExpression("hashCode()"));
    //producerProperties.PartitionCount = 2;

    //MessageChannel partOutputChannel = createBindableChannel("output",
    //        createProducerBindingProperties(producerProperties));
    //Binding<MessageChannel> partlate0ProducerBinding = binder
    //        .bindProducer("partlate.0", partOutputChannel, producerProperties);

    //QueueChannel partInputChannel0 = new QueueChannel();
    //QueueChannel partInputChannel1 = new QueueChannel();

    //ExtendedConsumerProperties<RabbitConsumerProperties> partLateConsumerProperties = createConsumerProperties();
    //partLateConsumerProperties.Extension.Prefix = "latebinder.";
    //partLateConsumerProperties.Partitioned = true;
    //partLateConsumerProperties.InstanceIndex = 0;
    //Binding<MessageChannel> partlate0Consumer0Binding = binder.bindConsumer(
    //        "partlate.0", "test", partInputChannel0, partLateConsumerProperties);
    //partLateConsumerProperties.InstanceIndex = 1;
    //Binding<MessageChannel> partlate0Consumer1Binding = binder.bindConsumer(
    //        "partlate.0", "test", partInputChannel1, partLateConsumerProperties);

    //ExtendedProducerProperties<RabbitProducerProperties> noDlqProducerProperties = createProducerProperties();
    //noDlqProducerProperties.Extension.Prefix = "latebinder.";
    //MessageChannel noDLQOutputChannel = createBindableChannel("output",
    //        createProducerBindingProperties(noDlqProducerProperties));
    //Binding<MessageChannel> noDlqProducerBinding = binder.bindProducer("lateNoDLQ.0",
    //        noDLQOutputChannel, noDlqProducerProperties);

    //QueueChannel noDLQInputChannel = new QueueChannel();
    //ExtendedConsumerProperties<RabbitConsumerProperties> noDlqConsumerProperties = createConsumerProperties();
    //noDlqConsumerProperties.Extension.Prefix = "latebinder.";
    //Binding<MessageChannel> noDlqConsumerBinding = binder.bindConsumer("lateNoDLQ.0",
    //        "test", noDLQInputChannel, noDlqConsumerProperties);

    //MessageChannel outputChannel = createBindableChannel("output",
    //        createProducerBindingProperties(noDlqProducerProperties));
    //Binding<MessageChannel> pubSubProducerBinding = binder.bindProducer("latePubSub",
    //        outputChannel, noDlqProducerProperties);
    //QueueChannel pubSubInputChannel = new QueueChannel();
    //noDlqConsumerProperties.Extension.DurableSubscription = false;
    //Binding<MessageChannel> nonDurableConsumerBinding = binder.bindConsumer(
    //        "latePubSub", "lategroup", pubSubInputChannel, noDlqConsumerProperties);
    //QueueChannel durablePubSubInputChannel = new QueueChannel();
    //noDlqConsumerProperties.Extension.DurableSubscription = true;
    //Binding<MessageChannel> durableConsumerBinding = binder.bindConsumer("latePubSub",
    //        "lateDurableGroup", durablePubSubInputChannel, noDlqConsumerProperties);

    //proxy.start();

    //moduleOutputChannel.send(MessageBuilder.withPayload("foo")
    //        .Header = MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN
    //        .build());
    //Message <?> message = moduleInputChannel.receive(10000);
    //assertThat(message).isNotNull();
    //assertThat(message.getPayload()).isNotNull();

    //noDLQOutputChannel.send(MessageBuilder.withPayload("bar")
    //        .Header = MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN
    //        .build());
    //message = noDLQInputChannel.receive(10000);
    //assertThat(message);
    //assertThat(message.getPayload()).isEqualTo("bar".getBytes());

    //outputChannel.send(MessageBuilder.withPayload("baz")
    //        .Header = MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN
    //        .build());
    //message = pubSubInputChannel.receive(10000);
    //assertThat(message);
    //assertThat(message.getPayload()).isEqualTo("baz".getBytes());
    //message = durablePubSubInputChannel.receive(10000);
    //assertThat(message).isNotNull();
    //assertThat(message.getPayload()).isEqualTo("baz".getBytes());

    //partOutputChannel.send(MessageBuilder.withPayload("0")
    //        .Header = MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN
    //        .build());
    //partOutputChannel.send(MessageBuilder.withPayload("1")
    //        .Header = MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN
    //        .build());
    //message = partInputChannel0.receive(10000);
    //assertThat(message).isNotNull();
    //assertThat(message.getPayload()).isEqualTo("0".getBytes());
    //message = partInputChannel1.receive(10000);
    //assertThat(message).isNotNull();
    //assertThat(message.getPayload()).isEqualTo("1".getBytes());

    //late0ProducerBinding.unbind();
    //late0ConsumerBinding.unbind();
    //partlate0ProducerBinding.unbind();
    //partlate0Consumer0Binding.unbind();
    //partlate0Consumer1Binding.unbind();
    //noDlqProducerBinding.unbind();
    //noDlqConsumerBinding.unbind();
    //pubSubProducerBinding.unbind();
    //nonDurableConsumerBinding.unbind();
    //durableConsumerBinding.unbind();

    //binder.cleanup();

    //proxy.stop();
    //cf.destroy();

    //this.rabbitAvailableRule.getResource().destroy();
    //	}

    //	@Test
    //    public void testBadUserDeclarationsFatal() throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    ConfigurableApplicationContext context = binder.getApplicationContext();
    //    ConfigurableListableBeanFactory bf = context.getBeanFactory();
    //    bf.registerSingleton("testBadUserDeclarationsFatal",

    //                new Queue("testBadUserDeclarationsFatal", false));
    //bf.registerSingleton("binder", binder);
    //RabbitExchangeQueueProvisioner provisioner = TestUtils.getPropertyValue(binder,
    //        "binder.provisioningProvider", RabbitExchangeQueueProvisioner.class);
    //bf.initializeBean(provisioner, "provisioner");
    //bf.registerSingleton("provisioner", provisioner);
    //context.addApplicationListener(provisioner);
    //RabbitAdmin admin = new RabbitAdmin(rabbitAvailableRule.getResource());
    //admin.declareQueue(new Queue("testBadUserDeclarationsFatal"));
    //// reset the connection and configure the "user" admin to auto declare queues...
    //rabbitAvailableRule.getResource().resetConnection();
    //bf.initializeBean(admin, "rabbitAdmin");
    //bf.registerSingleton("rabbitAdmin", admin);
    //admin.afterPropertiesSet();
    //// the mis-configured queue should be fatal
    //Binding <?> binding = null;
    //try
    //{
    //    binding = binder.bindConsumer("input", "baddecls",
    //            this.createBindableChannel("input", new BindingProperties()),
    //            createConsumerProperties());
    //    fail("Expected exception");
    //}
    //catch (BinderException e)
    //{
    //    assertThat(e.getCause()).isInstanceOf(AmqpIOException.class);
    //		}

    //        finally
    //{
    //    admin.deleteQueue("testBadUserDeclarationsFatal");
    //    if (binding != null)
    //    {
    //        binding.unbind();
    //    }
    //}
    //	}

    //	@Test
    //    public void testRoutingKeyExpression() throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    ExtendedProducerProperties<RabbitProducerProperties> producerProperties = createProducerProperties();
    //    producerProperties.Extension.setRoutingKeyExpression(
    //				spelExpressionParser.parseExpression("payload.field"));

    //    DirectChannel output = createBindableChannel("output",
    //				createProducerBindingProperties(producerProperties));
    //    output.BeanName = "rkeProducer";
    //    Binding<MessageChannel> producerBinding = binder.bindProducer("rke", output,
    //				producerProperties);

    //    RabbitAdmin admin = new RabbitAdmin(this.rabbitAvailableRule.getResource());
    //Queue queue = new AnonymousQueue();
    //TopicExchange exchange = new TopicExchange("rke");
    //org.springframework.amqp.core.Binding binding = BindingBuilder.bind(queue)
    //        .to(exchange).with("rkeTest");
    //admin.declareQueue(queue);
    //admin.declareBinding(binding);

    //output.addInterceptor(new ChannelInterceptor() {

    //            @Override

    //            public Message<?> preSend(Message<?> message, MessageChannel channel)
    //{
    //    assertThat(message.getHeaders()
    //            .get(RabbitExpressionEvaluatingInterceptor.ROUTING_KEY_HEADER))
    //            .isEqualTo("rkeTest");
    //    return message;
    //}

    //		});

    //output.send(new GenericMessage<>(new Pojo("rkeTest")));

    //Object out = spyOn(queue.getName()).receive(false);
    //assertThat(out).isInstanceOf(byte[].class);
    //assertThat(new String((byte[]) out, StandardCharsets.UTF_8))
    //        .isEqualTo("{\"field\":\"rkeTest\"}");

    //producerBinding.unbind();
    //	}

    //	@Test
    //    public void testRoutingKeyExpressionPartitionedAndDelay() throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    ExtendedProducerProperties<RabbitProducerProperties> producerProperties = createProducerProperties();
    //    producerProperties.Extension.setRoutingKeyExpression(
    //				spelExpressionParser.parseExpression("#root.getPayload().field"));
    //    // requires delayed message exchange plugin; tested locally
    //    // producerProperties.Extension.DelayedExchange = true;
    //    producerProperties.Extension
    //				.DelayExpression(spelExpressionParser.parseExpression = "1000");
    //    producerProperties.PartitionKeyExpression(new ValueExpression<> = 0);

    //DirectChannel output = createBindableChannel("output",
    //        createProducerBindingProperties(producerProperties));
    //output.BeanName = "rkeProducer";
    //Binding<MessageChannel> producerBinding = binder.bindProducer("rkep", output,
    //        producerProperties);

    //RabbitAdmin admin = new RabbitAdmin(this.rabbitAvailableRule.getResource());
    //Queue queue = new AnonymousQueue();
    //TopicExchange exchange = new TopicExchange("rkep");
    //org.springframework.amqp.core.Binding binding = BindingBuilder.bind(queue)
    //        .to(exchange).with("rkepTest-0");
    //admin.declareQueue(queue);
    //admin.declareBinding(binding);

    //output.addInterceptor(new ChannelInterceptor() {

    //            @Override

    //            public Message<?> preSend(Message<?> message, MessageChannel channel)
    //{
    //    assertThat(message.getHeaders()
    //            .get(RabbitExpressionEvaluatingInterceptor.ROUTING_KEY_HEADER))
    //            .isEqualTo("rkepTest");
    //    assertThat(message.getHeaders()
    //            .get(RabbitExpressionEvaluatingInterceptor.DELAY_HEADER))
    //            .isEqualTo(1000);
    //    return message;
    //}

    //		});

    //output.send(new GenericMessage<>(new Pojo("rkepTest")));

    //Object out = spyOn(queue.getName()).receive(false);
    //assertThat(out).isInstanceOf(byte[].class);
    //assertThat(new String((byte[]) out, StandardCharsets.UTF_8))
    //        .isEqualTo("{\"field\":\"rkepTest\"}");

    //producerBinding.unbind();
    //	}

    //	@Test
    //    public void testPolledConsumer() throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    PollableSource<MessageHandler> inboundBindTarget = new DefaultPollableMessageSource(
    //            this.messageConverter);
    //Binding<PollableSource<MessageHandler>> binding = binder.bindPollableConsumer(
    //        "pollable", "group", inboundBindTarget, createConsumerProperties());
    //RabbitTemplate template = new RabbitTemplate(
    //        this.rabbitAvailableRule.getResource());
    //template.convertAndSend("pollable.group", "testPollable");
    //boolean polled = inboundBindTarget.poll(m-> {

    //    assertThat(m.getPayload()).isEqualTo("testPollable");
    //		});
    //int n = 0;
    //while (n++ < 100 && !polled)
    //{
    //    polled = inboundBindTarget.poll(m-> {
    //        assertThat(m.getPayload()).isEqualTo("testPollable");
    //    });
    //}
    //assertThat(polled).isTrue();
    //binding.unbind();
    //	}

    //	@Test
    //    public void testPolledConsumerRequeue() throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    PollableSource<MessageHandler> inboundBindTarget = new DefaultPollableMessageSource(
    //            this.messageConverter);
    //ExtendedConsumerProperties<RabbitConsumerProperties> properties = createConsumerProperties();
    //Binding<PollableSource<MessageHandler>> binding = binder.bindPollableConsumer(
    //        "pollableRequeue", "group", inboundBindTarget, properties);
    //RabbitTemplate template = new RabbitTemplate(
    //        this.rabbitAvailableRule.getResource());
    //template.convertAndSend("pollableRequeue.group", "testPollable");
    //try
    //{
    //    boolean polled = false;
    //    int n = 0;
    //    while (n++ < 100 && !polled)
    //    {
    //        polled = inboundBindTarget.poll(m-> {
    //            assertThat(m.getPayload()).isEqualTo("testPollable");
    //            throw new RequeueCurrentMessageException();
    //        });
    //    }
    //}
    //catch (MessageHandlingException e)
    //{
    //    assertThat(e.getCause()).isInstanceOf(RequeueCurrentMessageException.class);
    //		}
    //		boolean polled = inboundBindTarget.poll(m-> {

    //            assertThat(m.getPayload()).isEqualTo("testPollable");
    //		});
    //assertThat(polled).isTrue();
    //binding.unbind();
    //	}

    //	@Test
    //    public void testPolledConsumerWithDlq() throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    PollableSource<MessageHandler> inboundBindTarget = new DefaultPollableMessageSource(
    //            this.messageConverter);
    //ExtendedConsumerProperties<RabbitConsumerProperties> properties = createConsumerProperties();
    //properties.MaxAttempts = 2;
    //properties.BackOffInitialInterval = 0;
    //properties.Extension.AutoBindDlq = true;
    //Binding<PollableSource<MessageHandler>> binding = binder.bindPollableConsumer(
    //        "pollableDlq", "group", inboundBindTarget, properties);
    //RabbitTemplate template = new RabbitTemplate(
    //        this.rabbitAvailableRule.getResource());
    //template.convertAndSend("pollableDlq.group", "testPollable");
    //try
    //{
    //    int n = 0;
    //    while (n++ < 100)
    //    {
    //        inboundBindTarget.poll(m-> {
    //            throw new RuntimeException("test DLQ");
    //        });
    //        Thread.sleep(100);
    //    }
    //}
    //catch (MessageHandlingException e)
    //{
    //    assertThat(
    //            e.getCause().getCause().getCause().getCause().getCause().getMessage())
    //            .isEqualTo("test DLQ");
    //}
    //org.springframework.amqp.core.Message deadLetter = template
    //        .receive("pollableDlq.group.dlq", 10_000);
    //assertThat(deadLetter).isNotNull();
    //binding.unbind();
    //	}

    //	@Test
    //    public void testPolledConsumerWithDlqNoRetry() throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    PollableSource<MessageHandler> inboundBindTarget = new DefaultPollableMessageSource(
    //            this.messageConverter);
    //ExtendedConsumerProperties<RabbitConsumerProperties> properties = createConsumerProperties();
    //properties.MaxAttempts = 1;
    //// properties.Extension.RequeueRejected = true; // loops, correctly
    //properties.Extension.AutoBindDlq = true;
    //Binding<PollableSource<MessageHandler>> binding = binder.bindPollableConsumer(
    //        "pollableDlqNoRetry", "group", inboundBindTarget, properties);
    //RabbitTemplate template = new RabbitTemplate(
    //        this.rabbitAvailableRule.getResource());
    //template.convertAndSend("pollableDlqNoRetry.group", "testPollable");
    //try
    //{
    //    int n = 0;
    //    while (n++ < 100)
    //    {
    //        inboundBindTarget.poll(m-> {
    //            throw new RuntimeException("test DLQ");
    //        });
    //        Thread.sleep(100);
    //    }
    //}
    //catch (MessageHandlingException e)
    //{
    //    assertThat(e.getCause().getMessage()).isEqualTo("test DLQ");
    //}
    //org.springframework.amqp.core.Message deadLetter = template
    //        .receive("pollableDlqNoRetry.group.dlq", 10_000);
    //assertThat(deadLetter).isNotNull();
    //binding.unbind();
    //	}

    //	@Test
    //    public void testPolledConsumerWithDlqRePub() throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    PollableSource<MessageHandler> inboundBindTarget = new DefaultPollableMessageSource(
    //            this.messageConverter);
    //ExtendedConsumerProperties<RabbitConsumerProperties> properties = createConsumerProperties();
    //properties.MaxAttempts = 2;
    //properties.BackOffInitialInterval = 0;
    //properties.Extension.AutoBindDlq = true;
    //properties.Extension.RepublishToDlq = true;
    //Binding<PollableSource<MessageHandler>> binding = binder.bindPollableConsumer(
    //        "pollableDlqRePub", "group", inboundBindTarget, properties);
    //RabbitTemplate template = new RabbitTemplate(
    //        this.rabbitAvailableRule.getResource());
    //template.convertAndSend("pollableDlqRePub.group", "testPollable");
    //boolean polled = false;
    //int n = 0;
    //while (n++ < 100 && !polled)
    //{
    //    Thread.sleep(100);
    //    polled = inboundBindTarget.poll(m-> {
    //        throw new RuntimeException("test DLQ");
    //    });
    //}
    //assertThat(polled).isTrue();
    //org.springframework.amqp.core.Message deadLetter = template
    //        .receive("pollableDlqRePub.group.dlq", 10_000);
    //assertThat(deadLetter).isNotNull();
    //binding.unbind();
    //	}

    //	@Test
    //    public void testCustomBatchingStrategy() throws Exception
    //{
    //    RabbitTestBinder binder = getBinder();
    //    ExtendedProducerProperties<RabbitProducerProperties> producerProperties = createProducerProperties();
    //    producerProperties.Extension.DeliveryMode = MessageDeliveryMode.NON_PERSISTENT;
    //    producerProperties.Extension.BatchingEnabled = true;
    //    producerProperties.Extension.BatchingStrategyBeanName = "testCustomBatchingStrategy";
    //    producerProperties.RequiredGroups = "default";

    //    ConfigurableListableBeanFactory beanFactory = binder.getApplicationContext().getBeanFactory();
    //    beanFactory.registerSingleton("testCustomBatchingStrategy", new TestBatchingStrategy());

    //DirectChannel output = createBindableChannel("output", createProducerBindingProperties(producerProperties));
    //output.BeanName = "batchingProducer";
    //Binding<MessageChannel> producerBinding = binder.bindProducer("batching.0", output, producerProperties);

    //Log logger = spy(TestUtils.getPropertyValue(binder, "binder.compressingPostProcessor.logger", Log.class));
    //new DirectFieldAccessor(TestUtils.getPropertyValue(binder, "binder.compressingPostProcessor"))
    //        .PropertyValue = "logger", logger;
    //when(logger.isTraceEnabled()).thenReturn(true);

    //assertThat(TestUtils.getPropertyValue(binder, "binder.compressingPostProcessor.level"))
    //        .isEqualTo(Deflater.BEST_SPEED);

    //output.send(new GenericMessage<>("0".getBytes()));
    //output.send(new GenericMessage<>("1".getBytes()));
    //output.send(new GenericMessage<>("2".getBytes()));
    //output.send(new GenericMessage<>("3".getBytes()));
    //output.send(new GenericMessage<>("4".getBytes()));

    //Object out = spyOn("batching.0.default").receive(false);
    //assertThat(out).isInstanceOf(byte[].class);
    //assertThat(new String((byte[]) out)).isEqualTo("0\u0000\n1\u0000\n2\u0000\n3\u0000\n4\u0000\n");

    //producerBinding.unbind();
    //}


    private DirectMessageListenerContainer VerifyContainer(RabbitInboundChannelAdapter endpoint)
        {
            DirectMessageListenerContainer container;
            RetryTemplate retry;
            container = GetPropertyValue<DirectMessageListenerContainer>(endpoint, "MessageListenerContainer");

            Assert.Equal(AcknowledgeMode.NONE, container.AcknowledgeMode);
            Assert.StartsWith("foo.props.0", container.GetQueueNames()[0]);
            //      assertThat(container.getQueueNames()[0]).startsWith("foo.props.0");
            Assert.False(container.IsChannelTransacted);
            //      assertThat(TestUtils.getPropertyValue(container, "transactional", Boolean.class))
            //		.isFalse();
            Assert.Equal(2, container.ConsumersPerQueue);
            //      assertThat(TestUtils.getPropertyValue(container, "concurrentConsumers"))
            //		.isEqualTo(2);
            //      assertThat(TestUtils.getPropertyValue(container, "maxConcurrentConsumers"))
            //		.isEqualTo(3);

            Assert.False(container.DefaultRequeueRejected);
            //      assertThat(TestUtils.getPropertyValue(container, "defaultRequeueRejected",
            //              Boolean.class)).isFalse();
            Assert.Equal(20, container.PrefetchCount);
            //      assertThat(TestUtils.getPropertyValue(container, "prefetchCount")).isEqualTo(20);
            retry = endpoint.RetryTemplate;
            //      retry = TestUtils.getPropertyValue(endpoint, "retryTemplate",
            //		RetryTemplate.class);
            Assert.Equal(23, GetFieldValue<int>(retry, "_maxAttempts"));
            //assertThat(TestUtils.getPropertyValue(retry, "retryPolicy.maxAttempts"))
            //		.isEqualTo(23);
            Assert.Equal(2000, GetFieldValue<int>(retry, "_backOffInitialInterval"));
            Assert.Equal(20000, GetFieldValue<int>(retry, "_backOffMaxInterval"));
            Assert.Equal(5.0, GetFieldValue<double>(retry, "_backOffMultiplier"));
            //      assertThat(TestUtils.getPropertyValue(retry, "backOffPolicy.initialInterval"))
            //		.isEqualTo(2000L);
            //      assertThat(TestUtils.getPropertyValue(retry, "backOffPolicy.maxInterval"))
            //		.isEqualTo(20000L);
            //      assertThat(TestUtils.getPropertyValue(retry, "backOffPolicy.multiplier"))
            //		.isEqualTo(5.0);
            //endpoint.
        //    var requestMatchers = GetPropertyValue(endpoint, "headerMapper.requestHeaderMatcher.matchers", List.class);
            //assertThat(requestMatchers).hasSize(1);
            //      assertThat(TestUtils.getPropertyValue(requestMatchers.get(0), "pattern"))
            //		.isEqualTo("foo");

            return container;
        }


    private class TestMessageHandler : IMessageHandler
        {
            private readonly CountdownEvent _latch;// = new CountdownEvent(3);

            public TestMessageHandler(int count)
            {
                _latch = new CountdownEvent(count);
            }

            public string ServiceName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public void HandleMessage(IMessage message)
            {
                _latch.Signal();
                throw new Exception();
            }

            public bool Wait() => _latch.Wait(TimeSpan.FromSeconds(10));
        }


        private void Cleanup(RabbitTestBinder binder)
        {
            binder.CoreBinder.ConnectionFactory.Destroy();
            binder.Cleanup();
            _cachingConnectionFactory = null;
        }
    }
}
