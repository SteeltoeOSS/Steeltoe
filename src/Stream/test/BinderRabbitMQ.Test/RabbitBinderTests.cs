using EasyNetQ.Management.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Rabbit.Inbound;
using Steeltoe.Integration.Rabbit.Outbound;
using Steeltoe.Integration.Rabbit.Support;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.RabbitMQ;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener;
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
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static Steeltoe.Messaging.RabbitMQ.Connection.CachingConnectionFactory;
using RabbitBinding = Steeltoe.Messaging.RabbitMQ.Config.Binding;

namespace Steeltoe.Stream.Binder.Rabbit
{
    [CollectionDefinition("BinderTests", DisableParallelization = true)]
    [Collection("BinderTests")]
    public partial class RabbitBinderTests
    {
        [Fact]
        public void TestSendAndReceiveBad()
        {
            var ccf = GetResource();
            var binder = GetBinder();

            var moduleOutputChannel = CreateBindableChannel("output", GetDefaultBindingOptions()) ;
            var moduleInputChannel = CreateBindableChannel("input", GetDefaultBindingOptions());
            var producerBinding = binder.BindProducer("bad.0", moduleOutputChannel, CreateProducerOptions());

            var endpoint = GetFieldValue<RabbitOutboundEndpoint>(producerBinding, "_lifecycle");

            Assert.True(endpoint.HeadersMappedLast);
            Assert.Contains("Passthrough", endpoint.Template.MessageConverter.GetType().Name);

            var consumerProps = (ExtendedConsumerOptions<RabbitConsumerOptions>)CreateConsumerOptions();
            consumerProps.Extension.ContainerType = ContainerType.DIRECT;

            var consumerBinding = binder.BindConsumer("bad.0", "test", moduleInputChannel, consumerProps);

            var inbound = GetFieldValue<RabbitInboundChannelAdapter>(consumerBinding, "_lifecycle");
            Assert.Contains("Passthrough", inbound.MessageConverter.GetType().Name);
            var container = GetPropertyValue<DirectMessageListenerContainer>(inbound, "MessageListenerContainer");
            Assert.NotNull(container);

            var message = MessageBuilder.WithPayload("bad".GetBytes())
                .SetHeader(MessageHeaders.CONTENT_TYPE, "foo/bar")
                .Build();

            var latch = new CountdownEvent(3);
            moduleInputChannel.Subscribe(new TestMessageHandler()
            {
                OnHandleMessage = (message) =>
                {
                    latch.Signal();
                    throw new Exception();
                }
            });
            moduleOutputChannel.Send(message);

            Assert.True(latch.Wait(TimeSpan.FromSeconds(10)));

            producerBinding.Unbind();
            consumerBinding.Unbind();
        }

        [Fact] // TODO: SPEL
        public void TestProducerErrorChannel()
        {
            var ccf = GetResource();
            ccf.IsPublisherConfirms = true;
            ccf.PublisherConfirmType = CachingConnectionFactory.ConfirmType.CORRELATED;
            ccf.ResetConnection();
            var binder = GetBinder();

            // Setup binder configuration to mimic autoconfig
            RegisterGlobalErrorChannel(binder);

            var moduleOutputChannel = CreateBindableChannel("output", GetDefaultBindingOptions());
            var producerOptions = CreateProducerOptions();
            producerOptions.ErrorChannelEnabled = true;

            var producerBinding = binder.BindProducer("ec.0", moduleOutputChannel, producerOptions);

            var message = MessageBuilder.WithPayload("bad".GetBytes())
               .SetHeader(MessageHeaders.CONTENT_TYPE, "foo/bar")
               .Build();

            var ec = binder.ApplicationContext.GetService<PublishSubscribeChannel>("ec.0.errors");
            Assert.NotNull(ec);
            var errorMessage = new AtomicReference<IMessage>();

            var latch = new CountdownEvent(2);
            ec.Subscribe(new TestMessageHandler()
            {
                OnHandleMessage = (message) =>
                {
                    errorMessage.GetAndSet(message);
                    latch.Signal();
                }
            });

            var globalEc = binder.ApplicationContext.GetService<ISubscribableChannel>(IntegrationContextUtils.ERROR_CHANNEL_BEAN_NAME);

            globalEc.Subscribe(new TestMessageHandler()
            {

                OnHandleMessage = (message) =>
                {
                    latch.Signal();
                }
            });

            moduleOutputChannel.Send(message);
            Assert.True(latch.Wait(TimeSpan.FromSeconds(10)));
            Assert.IsAssignableFrom<ErrorMessage>(errorMessage.Value);
            Assert.IsAssignableFrom<ReturnedRabbitMessageException>(errorMessage.Value.Payload);

            var exception = (ReturnedRabbitMessageException)errorMessage.Value.Payload;
            Assert.Equal(312, exception.ReplyCode);
            Assert.Equal("NO_ROUTE", exception.ReplyText);

            // SPEL
            //    var endpoint = GetPropertyValue<RabbitOutboundEndpoint>(producerBinding, "lifecycle");
            //  Assert.Equal("#root", GetPropertyValue<string>(endpoint, "confirmCorrelationExpression.expression"));


            //        class WrapperAccessor extends AmqpOutboundEndpoint
            //        {

            //            WrapperAccessor(AmqpTemplate amqpTemplate) {
            //				super(amqpTemplate);
            //    }

            //    CorrelationDataWrapper getWrapper()
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
            //producerBinding.Unbind();
        }

        [Fact] // TODO: SPEL
        public void TestProducerAckChannel()
        {
            var binder = GetBinder();
            var ccf = GetResource();
            ccf.IsPublisherReturns = true;
            ccf.PublisherConfirmType = ConfirmType.CORRELATED;
            ccf.ResetConnection();

            var moduleOutputChannel = CreateBindableChannel("output", GetDefaultBindingOptions());
            var producerProps = CreateProducerOptions() as ExtendedProducerOptions<RabbitProducerOptions>;
            producerProps.ErrorChannelEnabled = true;
            producerProps.Extension.ConfirmAckChannel = "acksChannel";

            var producerBinding = binder.BindProducer("acks.0", moduleOutputChannel, producerProps);
            var messageBytes = "acksMessage".GetBytes();
            var message = MessageBuilder.WithPayload(messageBytes).Build();

            var confirm = new AtomicReference<IMessage>();
            var confirmLatch = new CountdownEvent(1);
            binder.ApplicationContext.GetService<DirectChannel>("acksChannel")
                            .Subscribe(new TestMessageHandler()
                            {
                                OnHandleMessage = (m) =>
                                {
                                    confirm.GetAndSet(m);
                                    confirmLatch.Signal();
                                }
                            });
            moduleOutputChannel.Send(message);
            Assert.True(confirmLatch.Wait(TimeSpan.FromSeconds(10000)));
            //       Assert.Equal(messageBytes, confirm.Value.Payload); TODO: SPEL
            producerBinding.Unbind();
        }

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
            consumerOptions.PostProcess();

            var consumerBinding = binder.BindConsumer("props.0", null, CreateBindableChannel("input", GetDefaultBindingOptions()), properties);

            var endpoint = ExtractEndpoint(consumerBinding) as RabbitInboundChannelAdapter;
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
            consumerBinding = binder.BindConsumer("props.0", "test", CreateBindableChannel("input", GetDefaultBindingOptions()), properties);

            endpoint = ExtractEndpoint(consumerBinding) as RabbitInboundChannelAdapter;
            container = VerifyContainer(endpoint);

            Assert.Equal("foo.props.0.test", container.GetQueueNames()[0]);

            consumerBinding.Unbind();
            Assert.False(endpoint.IsRunning);
        }

        [Fact]
        public void TestMultiplexOnPartitionedConsumerWithMultipleDestinations()
        {
            var consumerProperties = CreateConsumerOptions();
            var proxy = new RabbitProxy(LoggerFactory.CreateLogger<RabbitProxy>());
            var port = proxy.Port;
            var ccf = new CachingConnectionFactory("localhost", port);

            var rabbitExchangeQueueProvisioner = new RabbitExchangeQueueProvisioner(ccf, new RabbitBindingsOptions(), GetBinder().ApplicationContext, LoggerFactory.CreateLogger<RabbitExchangeQueueProvisioner>());

            consumerProperties.Multiplex = true;
            consumerProperties.Partitioned = true;
            consumerProperties.InstanceIndexList = new int[] { 1, 2, 3 }.ToList();

            var consumerDestination = rabbitExchangeQueueProvisioner.ProvisionConsumerDestination("foo,qaa", "boo", consumerProperties);

            proxy.Stop();
            Assert.Equal("foo.boo-1,foo.boo-2,foo.boo-3,qaa.boo-1,qaa.boo-2,qaa.boo-3", consumerDestination.Name);

        }

        [Fact]
        public async void TestConsumerPropertiesWithUserInfrastructureNoBind()
        {
            var logger = LoggerFactory.CreateLogger<RabbitAdmin>();
            var admin = new RabbitAdmin(RabbitTestBinder.GetApplicationContext(), GetResource(), logger);
            var queue = new Queue("propsUser1.infra");
            admin.DeclareQueue(queue);

            DirectExchange exchange = new DirectExchange("propsUser1");
            admin.DeclareExchange(exchange);
            admin.DeclareBinding(BindingBuilder.Bind(queue).To(exchange).With("foo"));

            var binder = GetBinder();
            var properties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            properties.Extension.DeclareExchange = false;
            properties.Extension.BindQueue = false;

            var consumerBinding = binder.BindConsumer("propsUser1", "infra", CreateBindableChannel("input", GetDefaultBindingOptions()), properties);

            var endpoint = ExtractEndpoint(consumerBinding);
            var container = GetPropertyValue<DirectMessageListenerContainer>(endpoint, "MessageListenerContainer");

            Assert.False(container.MissingQueuesFatal);
            Assert.True(container.IsRunning);

            await consumerBinding.Unbind();

            Assert.False(container.IsRunning);

            var client = new HttpClient();
            var scheme = "http://";
            var vhost = "%2F";
            var byteArray = "guest:guest".GetBytes();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            var response = await client.GetAsync($"{scheme}guest:guest@localhost:15672/api/exchanges/{vhost}/{exchange.ExchangeName}/bindings/source");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var jsonResult = await response.Content.ReadAsStringAsync();
            var foo = JsonConvert.DeserializeObject<List<ExpandoObject>>(jsonResult, new ExpandoObjectConverter());

            Assert.Single(foo);
        }

        [Fact]
        public void TestAnonWithBuiltInExchange()
        {
            var binder = GetBinder();
            var properties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            properties.Extension.DeclareExchange = false;
            properties.Extension.QueueNameGroupOnly = true;

            var consumerBinding = binder.BindConsumer("amq.topic", null, CreateBindableChannel("input", GetDefaultBindingOptions()), properties);
            var endpoint = ExtractEndpoint(consumerBinding);
            var container = GetPropertyValue<DirectMessageListenerContainer>(endpoint, "MessageListenerContainer");

            var queueName = container.GetQueueNames()[0];

            Assert.StartsWith("anonymous.", queueName);
            Assert.True(container.IsRunning);

            consumerBinding.Unbind();
            Assert.False(container.IsRunning);
        }

        [Fact]
        public void TestAnonWithBuiltInExchangeCustomPrefix()
        {
            var binder = GetBinder();
            var properties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            properties.Extension.DeclareExchange = false;
            properties.Extension.QueueNameGroupOnly = true;
            properties.Extension.AnonymousGroupPrefix = "customPrefix.";

            var consumerBinding = binder.BindConsumer("amq.topic", null, CreateBindableChannel("input", GetDefaultBindingOptions()), properties);
            var endpoint = ExtractEndpoint(consumerBinding);
            var container = GetPropertyValue<DirectMessageListenerContainer>(endpoint, "MessageListenerContainer");

            var queueName = container.GetQueueNames()[0];
            Assert.StartsWith("customPrefix.", queueName);
            Assert.True(container.IsRunning);

            consumerBinding.Unbind();
            Assert.False(container.IsRunning);
        }

        [Fact]
        public async Task TestConsumerPropertiesWithUserInfrastructureCustomExchangeAndRK()
        {
            var binder = GetBinder();
            var properties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            properties.Extension.ExchangeType = ExchangeType.DIRECT;
            properties.Extension.BindingRoutingKey = "foo,bar";
            properties.Extension.BindingRoutingKeyDelimiter = ",";
            properties.Extension.QueueNameGroupOnly = true;
            // properties.Extension.DelayedExchange = true; // requires delayed message
            // exchange plugin; tested locally

            var group = "infra";
            var consumerBinding = binder.BindConsumer("propsUser2", group, CreateBindableChannel("input", GetDefaultBindingOptions()), properties);
            var endpoint = ExtractEndpoint(consumerBinding);
            var container = GetPropertyValue<DirectMessageListenerContainer>(endpoint, "MessageListenerContainer");

            Assert.True(container.IsRunning);
            await consumerBinding.Unbind();

            Assert.False(container.IsRunning);
            Assert.Equal(group, container.GetQueueNames()[0]);

            var client = new Client();
            var bindings = await client.GetBindingsBySource("/", "propsUser2");
            int n = 0;
            while (n++ < 100 && (bindings == null || bindings.Count() < 1))
            {
                Thread.Sleep(100);
                bindings = await client.GetBindingsBySource("/", "propsUser2");
            }

            Assert.Equal(2, bindings.Count());

            Assert.Equal("propsUser2", bindings.ElementAt(0).Source);
            Assert.Equal(group, bindings.ElementAt(0).Destination);
            Assert.Contains(bindings.ElementAt(0).RoutingKey, new List<string>() { "foo", "bar" });

            Assert.Equal("propsUser2", bindings.ElementAt(1).Source);
            Assert.Equal(group, bindings.ElementAt(1).Destination);
            Assert.Contains(bindings.ElementAt(1).RoutingKey, new List<string>() { "foo", "bar" });
            Assert.NotEqual(bindings.ElementAt(1).RoutingKey, bindings.ElementAt(0).RoutingKey);

            var exchange = await client.GetExchange("/", "propsUser2");
            while (n++ < 100 && exchange == null)
            {
                Thread.Sleep(100);
                exchange = await client.GetExchange("/", "propsUser2");
            }

            Assert.Equal("direct", exchange.Type);
            Assert.True(exchange.Durable);
            Assert.False(exchange.AutoDelete);
        }

        [Fact]
        public async Task TestConsumerPropertiesWithUserInfrastructureCustomQueueArgs()
        {
            var binder = GetBinder();
            var properties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            var extProps = properties.Extension;

            extProps.ExchangeType = ExchangeType.DIRECT;
            extProps.ExchangeDurable = false;
            extProps.ExchangeAutoDelete = true;
            extProps.BindingRoutingKey = "foo";
            extProps.Expires = 30_000;
            extProps.Lazy = true;
            extProps.MaxLength = 10_000;
            extProps.MaxLengthBytes = 100_000;
            extProps.MaxPriority = 10;
            extProps.OverflowBehavior = "drop-head";
            extProps.Ttl = 2_000;
            extProps.AutoBindDlq = true;
            extProps.DeadLetterQueueName = "customDLQ";
            extProps.DeadLetterExchange = "customDLX";
            extProps.DeadLetterExchangeType = ExchangeType.TOPIC;
            extProps.DeadLetterRoutingKey = "customDLRK";
            extProps.DlqDeadLetterExchange = "propsUser3";

            // GH-259 - if the next line was commented, the test failed.
            extProps.DlqDeadLetterRoutingKey = "propsUser3";
            extProps.DlqExpires = 60_000;
            extProps.DlqLazy = true;
            extProps.DlqMaxLength = 20_000;
            extProps.DlqMaxLengthBytes = 40_000;
            extProps.DlqOverflowBehavior = "reject-publish";
            extProps.DlqMaxPriority = 8;
            extProps.DlqTtl = 1_000;
            extProps.ConsumerTagPrefix = "testConsumerTag";
            extProps.Exclusive = true;

            var consumerBinding = binder.BindConsumer("propsUser3", "infra", CreateBindableChannel("input", GetDefaultBindingOptions()), properties);
            var endpoint = ExtractEndpoint(consumerBinding);
            var container = GetPropertyValue<DirectMessageListenerContainer>(endpoint, "MessageListenerContainer");

            Assert.True(container.IsRunning);

            var client = new Client();
            var bindings = await client.GetBindingsBySource("/", "propsUser3");

            int n = 0;
            while (n++ < 100 && (bindings == null || bindings.Count() < 1))
            {
                Thread.Sleep(100);
                bindings = await client.GetBindingsBySource("/", "propsUser3");
            }

            Assert.Single(bindings);
            Assert.Equal("propsUser3", bindings.ElementAt(0).Source);
            Assert.Equal("propsUser3.infra", bindings.ElementAt(0).Destination);
            Assert.Equal("foo", bindings.ElementAt(0).RoutingKey);

            bindings = await client.GetBindingsBySource("/", "customDLX");
            n = 0;
            while (n++ < 100 && (bindings == null || bindings.Count() < 1))
            {
                Thread.Sleep(100);
                bindings = await client.GetBindingsBySource("/", "customDLX");
            }

            Assert.Equal("customDLX", bindings.ElementAt(0).Source);
            Assert.Equal("customDLQ", bindings.ElementAt(0).Destination);
            Assert.Equal("customDLRK", bindings.ElementAt(0).RoutingKey);

            var exchange = await client.GetExchange("/", "propsUser3");
            n = 0;
            while (n++ < 100 && exchange == null)
            {
                Thread.Sleep(100);
                exchange = await client.GetExchange("/", "propsUser3");
            }

            Assert.Equal("direct", exchange.Type);
            Assert.False(exchange.Durable);
            Assert.True(exchange.AutoDelete);

            exchange = await client.GetExchange("/", "customDLX");
            n = 0;
            while (n++ < 100 && exchange == null)
            {
                Thread.Sleep(100);
                exchange = await client.GetExchange("/", "customDLX");
            }

            Assert.Equal("topic", exchange.Type);
            Assert.True(exchange.Durable);
            Assert.False(exchange.AutoDelete);

            var queue = await client.GetQueue("/", "propsUser3.infra");
            n = 0;
            while (n++ < 100 && queue == null || queue.Consumers == 0)
            {
                Thread.Sleep(100);
                queue = await client.GetQueue("/", "propsUser3.infra");
            }

            Assert.NotNull(queue); 

            Assert.Equal("30000", queue.Arguments["x-expires"]);
            Assert.Equal("10000", queue.Arguments["x-max-length"]);
            Assert.Equal("100000", queue.Arguments["x-max-length-bytes"]);
            Assert.Equal("drop-head", queue.Arguments["x-overflow"]);
            Assert.Equal("10", queue.Arguments["x-max-priority"]);

            Assert.Equal("2000", queue.Arguments["x-message-ttl"]);
            Assert.Equal("customDLX", queue.Arguments["x-dead-letter-exchange"]);
            Assert.Equal("customDLRK", queue.Arguments["x-dead-letter-routing-key"]);

            Assert.Equal("lazy", queue.Arguments["x-queue-mode"]);
            Assert.Equal("testConsumerTag#0", queue.ExclusiveConsumerTag);

            queue = await client.GetQueue("/", "customDLQ");

            n = 0;
            while (n++ < 100 && queue == null)
            {
                Thread.Sleep(100);
                queue = await client.GetQueue("/", "customDLQ");
            
            }
            Assert.NotNull(queue);

            Assert.Equal("60000", queue.Arguments["x-expires"]);
            Assert.Equal("20000", queue.Arguments["x-max-length"]);
            Assert.Equal("40000", queue.Arguments["x-max-length-bytes"]);
            Assert.Equal("reject-publish", queue.Arguments["x-overflow"]);
            Assert.Equal("8", queue.Arguments["x-max-priority"]);

            Assert.Equal("1000", queue.Arguments["x-message-ttl"]);
            Assert.Equal("propsUser3", queue.Arguments["x-dead-letter-exchange"]);
            Assert.Equal("propsUser3", queue.Arguments["x-dead-letter-routing-key"]);

            Assert.Equal("lazy", queue.Arguments["x-queue-mode"]);

            await consumerBinding.Unbind();
            Assert.False(container.IsRunning);
            
        }

        [Fact]
        public async void TestConsumerPropertiesWithHeaderExchanges()
        {
            var binder = GetBinder();
            var properties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            properties.Extension.ExchangeType = ExchangeType.HEADERS;
            properties.Extension.AutoBindDlq = true;
            properties.Extension.DeadLetterExchange = ExchangeType.HEADERS;
            properties.Extension.DeadLetterExchange = "propsHeader.dlx";

            var queueBindingArguments = new Dictionary<string, string>();
            queueBindingArguments.Add("x-match", "any");
            queueBindingArguments.Add("foo", "bar");
            properties.Extension.QueueBindingArguments = queueBindingArguments;
            properties.Extension.DlqBindingArguments = queueBindingArguments;

            var group = "bindingArgs";
            var consumerBinding = binder.BindConsumer("propsHeader", group, CreateBindableChannel("input", GetDefaultBindingOptions()), properties);
            var endpoint = ExtractEndpoint(consumerBinding);
            var container = GetPropertyValue<DirectMessageListenerContainer>(endpoint, "MessageListenerContainer");

            Assert.True(container.IsRunning);
            await consumerBinding.Unbind();

            Assert.False(container.IsRunning);
            Assert.Equal("propsHeader." + group, container.GetQueueNames()[0]);

            var client = new Client();
            var bindings = await client.GetBindingsBySource("/", "propsHeader");

            int n = 0;
            while (n++ < 100 && (bindings == null || bindings.Count() < 1))
            {
                Thread.Sleep(100);
                bindings = await client.GetBindingsBySource("/", "propsHeader");
            }

            Assert.Single(bindings);
            var binding = bindings.First();
            Assert.Equal("propsHeader", binding.Source);
            Assert.Equal("propsHeader." + group, binding.Destination);
            Assert.Contains(binding.Arguments, (arg) => arg.Key == "x-match" && arg.Value == "any");
            Assert.Contains(binding.Arguments, (arg) => arg.Key == "foo" && arg.Value == "bar");

            bindings = await client.GetBindingsBySource("/", "propsHeader.dlx");
            n = 0;
            while (n++ < 100 && (bindings == null || bindings.Count() < 1))
            {
                Thread.Sleep(100);
                bindings = await client.GetBindingsBySource("/", "propsHeader.dlx");
            }

            Assert.Single(bindings);
            binding = bindings.First();
            Assert.Equal("propsHeader.dlx", binding.Source);
            Assert.Equal("propsHeader." + group + ".dlq", binding.Destination);
            Assert.Contains(binding.Arguments, (arg) => arg.Key == "x-match" && arg.Value == "any");
            Assert.Contains(binding.Arguments, (arg) => arg.Key == "foo" && arg.Value == "bar");
        }

        [Fact] // TODO: SPEL
        public void TestProducerProperties()
        {
            var binder = GetBinder();
            var bindingOptions = GetDefaultBindingOptions();

            var producerBinding = binder.BindProducer("props.0", CreateBindableChannel("input", bindingOptions), CreateProducerOptions());

            var endpoint = ExtractEndpoint(producerBinding) as RabbitOutboundEndpoint;
            Assert.Equal(MessageDeliveryMode.PERSISTENT, endpoint.DefaultDeliveryMode);


            //List <?> requestHeaders = TestUtils.getPropertyValue(endpoint,
            //        "headerMapper.requestHeaderMatcher.matchers", List.class);
            //assertThat(requestHeaders).hasSize(4);

            producerBinding.Unbind();
            Assert.False(endpoint.IsRunning);

            Assert.False(endpoint.Template.IsChannelTransacted);

            var producerProperties = CreateProducerOptions() as ExtendedProducerOptions<RabbitProducerOptions>;
            binder.ApplicationContext.Register("pkExtractor", new TestPartitionSupport("pkExtractor"));
            binder.ApplicationContext.Register("pkSelector", new TestPartitionSupport("pkSelector"));

            producerProperties.PartitionKeyExtractorName = "pkExtractor";
            producerProperties.PartitionSelectorName = "pkSelector";
            producerProperties.Extension.Prefix = "foo.";
            producerProperties.Extension.DeliveryMode = MessageDeliveryMode.NON_PERSISTENT;
            producerProperties.Extension.HeaderPatterns = new string[] { "foo" }.ToList();
            producerProperties.PartitionKeyExpression = "'foo'"; //(spelExpressionParser.parseExpression = "'foo'");
            producerProperties.PartitionSelectorExpression = "0";// spelExpressionParser.parseExpression("0"));
            producerProperties.PartitionCount = 1;
            producerProperties.Extension.Transacted = true;
            producerProperties.Extension.DelayExpression = "42"; // (spelExpressionParser.parseExpression = "42");
            producerProperties.RequiredGroups = new string[] { "prodPropsRequired" }.ToList();

            var producerBindingProperties = CreateProducerBindingOptions(producerProperties);
            var channel = CreateBindableChannel("output", producerBindingProperties);

            producerBinding = binder.BindProducer("props.0", channel, producerProperties);

            endpoint = ExtractEndpoint(producerBinding) as RabbitOutboundEndpoint;
            Assert.Same(GetResource(), endpoint.Template.ConnectionFactory);

            Assert.Equal("'props.0-' + headers['" + BinderHeaders.PARTITION_HEADER + "']", endpoint.RoutingKeyExpression.ExpressionString);
            Assert.Equal("42", endpoint.DelayExpression.ExpressionString);
            Assert.Equal(MessageDeliveryMode.NON_PERSISTENT, endpoint.DefaultDeliveryMode);
            Assert.True(endpoint.Template.IsChannelTransacted);

            VerifyFooRequestProducer(endpoint);
            var message = MessageBuilder.WithPayload("foo").Build();
            channel.Send(message);
            var received = new RabbitTemplate(GetResource()).Receive("foo.props.0.prodPropsRequired-0", 10_000);
            Assert.NotNull(received);

            Assert.Equal(42, received.Headers[RabbitMessageHeaders.RECEIVED_DELAY]);
            producerBinding.Unbind();
            Assert.False(endpoint.IsRunning);

        }

        [Fact]
        public void TestDurablePubSubWithAutoBindDLQ()
        {
            var logger = LoggerFactory.CreateLogger<RabbitAdmin>();
            var admin = new RabbitAdmin(GetResource(), logger);

            var binder = GetBinder();

            var consumerProperties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            consumerProperties.Extension.Prefix = TEST_PREFIX;
            consumerProperties.Extension.AutoBindDlq = true;
            consumerProperties.Extension.DurableSubscription = true;
            consumerProperties.MaxAttempts = 1; // disable retry
            var moduleInputChannel = CreateBindableChannel("input", CreateConsumerBindingOptions(consumerProperties));
            moduleInputChannel.ComponentName = "durableTest";

            moduleInputChannel.Subscribe(new TestMessageHandler()
            {
                OnHandleMessage = (message) =>
                {
                    throw new Exception("foo");
                }
            });

            var consumerBinding = binder.BindConsumer("durabletest.0", "tgroup", moduleInputChannel, consumerProperties);

            var template = new RabbitTemplate(GetResource());
            template.ConvertAndSend(TEST_PREFIX + "durabletest.0", string.Empty, "foo");

            int n = 0;
            while (n++ < 100)
            {
                var deadLetter = template.ReceiveAndConvert<string>(TEST_PREFIX + "durabletest.0.tgroup.dlq");
                if (deadLetter != null)
                {
                    Assert.Equal("foo", deadLetter);
                    break;
                }

                Thread.Sleep(100);
            }

            Assert.InRange(n, 0, 150);

            consumerBinding.Unbind();
            Assert.NotNull(admin.GetQueueProperties(TEST_PREFIX + "durabletest.0.tgroup.dlq"));
        }

        [Fact]
        public void TestNonDurablePubSubWithAutoBindDLQ()
        {
            var logger = LoggerFactory.CreateLogger<RabbitAdmin>();
            var admin = new RabbitAdmin(GetResource(), logger);

            var binder = GetBinder();
            var consumerProperties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            consumerProperties.Extension.Prefix = TEST_PREFIX;
            consumerProperties.Extension.AutoBindDlq = true;
            consumerProperties.Extension.DurableSubscription = false;
            consumerProperties.MaxAttempts = 1; // disable retry
            var bindingProperties = CreateConsumerBindingOptions(
                    consumerProperties);
            DirectChannel moduleInputChannel = CreateBindableChannel("input", bindingProperties);
            moduleInputChannel.ComponentName = "nondurabletest";
            moduleInputChannel.Subscribe(new TestMessageHandler()
            {
                OnHandleMessage = (message) =>
                {
                    throw new Exception("foo");
                }
            });

            var consumerBinding = binder.BindConsumer("nondurabletest.0", "tgroup", moduleInputChannel, consumerProperties);

            consumerBinding.Unbind();
            Assert.Null(admin.GetQueueProperties(TEST_PREFIX + "nondurabletest.0.dlq"));

        }

        [Fact]
        public void TestAutoBindDLQ()
        {
            var binder = GetBinder();
            var consumerProperties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            consumerProperties.Extension.Prefix = TEST_PREFIX;
            consumerProperties.Extension.AutoBindDlq = true;
            consumerProperties.MaxAttempts = 1; // disable retry
            consumerProperties.Extension.DurableSubscription = true;
            var bindingProperties = CreateConsumerBindingOptions(consumerProperties);
            DirectChannel moduleInputChannel = CreateBindableChannel("input", bindingProperties);
            moduleInputChannel.ComponentName = "dlqTest";
            moduleInputChannel.Subscribe(new TestMessageHandler()
            {
                OnHandleMessage = (message) =>
                {
                    throw new Exception("foo");
                }
            });
            consumerProperties.Multiplex = true;
            var consumerBinding = binder.BindConsumer("dlqtest,dlqtest2", "default", moduleInputChannel, consumerProperties);

            var endpoint = ExtractEndpoint(consumerBinding);
            var container = GetPropertyValue<AbstractMessageListenerContainer>(endpoint, "MessageListenerContainer");
            Assert.Equal(2, container.GetQueueNames().Length);

            var template = new RabbitTemplate(GetResource());
            template.ConvertAndSend(string.Empty, TEST_PREFIX + "dlqtest.default", "foo");

            int n = 0;
            while (n++ < 100)
            {
                string deadLetter = template.ReceiveAndConvert<string>(TEST_PREFIX + "dlqtest.default.dlq");
                if (deadLetter != null)
                {
                    Assert.Equal("foo", deadLetter);
                    break;
                }

                Thread.Sleep(100);
            }

            Assert.InRange(n, 0, 99);

            template.ConvertAndSend(string.Empty, TEST_PREFIX + "dlqtest2.default", "bar");

            n = 0;
            while (n++ < 100)
            {
                string deadLetter = template.ReceiveAndConvert<string>(TEST_PREFIX + "dlqtest2.default.dlq");
                if (deadLetter != null)
                {
                    Assert.Equal("bar", deadLetter);
                    break;
                }

                Thread.Sleep(100);
            }

            Assert.InRange(n, 0, 99);
            consumerBinding.Unbind();

            var provider = GetPropertyValue<RabbitExchangeQueueProvisioner>(binder.Binder, "ProvisioningProvider");
            var context = GetFieldValue<GenericApplicationContext>(provider, "_autoDeclareContext");

            Assert.False(context.ContainsService(TEST_PREFIX + "dlqtest.default.binding"));
            Assert.False(context.ContainsService(TEST_PREFIX + "dlqtest.default"));
            Assert.False(context.ContainsService(TEST_PREFIX + "dlqtest.default.dlq.binding"));
            Assert.False(context.ContainsService(TEST_PREFIX + "dlqtest.default.dlq"));
        }

        [Fact] // TODO
        public async void TestAutoBindDLQManualAcks()
        {
            var binder = GetBinder();
            var consumerProperties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            consumerProperties.Extension.Prefix = TEST_PREFIX;
            consumerProperties.Extension.AutoBindDlq = true;
            consumerProperties.MaxAttempts = 2;
            consumerProperties.Extension.DurableSubscription = true;
            consumerProperties.Extension.AcknowledgeMode = AcknowledgeMode.MANUAL;
            var bindingProperties = CreateConsumerBindingOptions(consumerProperties);

            DirectChannel moduleInputChannel = CreateBindableChannel("input", bindingProperties);
            moduleInputChannel.ComponentName = "dlqTestManual";

            var client = new Client();
            var vhost = client.GetVhost("/");

            moduleInputChannel.Subscribe(new TestMessageHandler()
            {
                OnHandleMessage = (message) =>
                {
                    var info = client.GetQueue(TEST_PREFIX + "dlqTestManual.default", vhost);
                    int n = 0;
                    while (n++ < 100 && info.MessagesUnacknowledged < 1L)
                    {
                        Thread.Sleep(100);
                        info = client.GetQueue(TEST_PREFIX + "dlqTestManual.default", vhost);
                    }

                    throw new Exception("foo");
                }
            });
            var consumerBinding = binder.BindConsumer("dlqTestManual", "default", moduleInputChannel, consumerProperties);

            //var template = new RabbitTemplate(GetResource());
            //template.ConvertAndSend(string.Empty, TEST_PREFIX + "dlqTestManual.default", "foo");

            //int n = 0;
            //while (n++ < 100)
            //{
            //    var deadLetter = template.ReceiveAndConvert<string>(TEST_PREFIX + "dlqTestManual.default.dlq");
            //    if (deadLetter != null)
            //    {
            //        Assert.Equal("foo", deadLetter);
            //        break;
            //    }

            //    Thread.Sleep(100);
            //}

            //Assert.InRange(n, 1, 100);

            //n = 0;
            //var info = client.GetQueue(TEST_PREFIX + "dlqTestManual.default", vhost);
            //while (n++ < 100 && info.MessagesUnacknowledged > 0L)
            //{
            //    Thread.Sleep(100);
            //    info = client.GetQueue(TEST_PREFIX + "dlqTestManual.default", vhost);
            //}

            //Assert.Equal(0, info.MessagesUnacknowledged);

            await consumerBinding.Unbind();

            var provider = GetPropertyValue<RabbitExchangeQueueProvisioner>(binder.Binder, "ProvisioningProvider");
            var context = GetFieldValue<GenericApplicationContext>(provider, "_autoDeclareContext");

            Assert.False(context.ContainsService(TEST_PREFIX + "dlqTestManual.default.binding"));
            Assert.False(context.ContainsService(TEST_PREFIX + "dlqTestManual.default"));
            Assert.False(context.ContainsService(TEST_PREFIX + "dlqTestManual.default.dlq.binding"));
            Assert.False(context.ContainsService(TEST_PREFIX + "dlqTestManual.default.dlq"));

           
        }

        [Fact]
        public void TestAutoBindDLQPartionedConsumerFirst()
        {
            var binder = GetBinder();
            var properties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            properties.Extension.Prefix = "bindertest.";
            properties.Extension.AutoBindDlq = true;
            properties.MaxAttempts = 1; // disable retry
            properties.Partitioned = true;
            properties.InstanceIndex = 0;

            DirectChannel input0 = CreateBindableChannel("input", CreateConsumerBindingOptions(properties));
            input0.ComponentName = "test.input0DLQ";
            var input0Binding = binder.BindConsumer("partDLQ.0", "dlqPartGrp", input0, properties);
            var defaultConsumerBinding1 = binder.BindConsumer("partDLQ.0", "default", new QueueChannel(), properties);
            properties.InstanceIndex = 1;

            DirectChannel input1 = CreateBindableChannel("input1", CreateConsumerBindingOptions(properties));
            input1.ComponentName = "test.input1DLQ";
            var input1Binding = binder.BindConsumer("partDLQ.0", "dlqPartGrp", input1, properties);

            var defaultConsumerBinding2 = binder.BindConsumer("partDLQ.0", "default", new QueueChannel(), properties);

            var producerProperties = CreateProducerOptions() as ExtendedProducerOptions<RabbitProducerOptions>;
            producerProperties.Extension.Prefix = "bindertest.";

            binder.ApplicationContext.Register("pkExtractor", new TestPartitionSupport("pkExtractor"));
            binder.ApplicationContext.Register("pkSelector", new TestPartitionSupport("pkSelector"));

            producerProperties.Extension.AutoBindDlq = true;
            producerProperties.PartitionKeyExtractorName = "pkExtractor";
            producerProperties.PartitionSelectorName = "pkSelector";
            producerProperties.PartitionCount = 2;

            var bindingProperties = CreateProducerBindingOptions(producerProperties);

            var output = CreateBindableChannel("output", bindingProperties);
            output.ComponentName = "test.output";
            var outputBinding = binder.BindProducer("partDLQ.0", output, producerProperties);

            var latch0 = new CountdownEvent(1);

            input0.Subscribe(new TestMessageHandler()
            {
                OnHandleMessage = (message) =>
                {
                    if (latch0.CurrentCount <= 0)
                    {
                        throw new Exception("dlq");
                    }

                    latch0.Signal();
                }
            });

            var latch1 = new CountdownEvent(1);

            input1.Subscribe(new TestMessageHandler()
            {
                OnHandleMessage = (message) =>
                {
                    if (latch1.CurrentCount <= 0)
                    {
                        throw new Exception("dlq");
                    }

                    latch1.Signal();
                }
            });
            var message = MessageBuilder.WithPayload(1).Build();
            output.Send(message);
            Assert.True(latch1.Wait(TimeSpan.FromSeconds(10)));

            output.Send(Message.Create(0));
            Assert.True(latch0.Wait(TimeSpan.FromSeconds(10)));

            output.Send(Message.Create(1));

            var template = new RabbitTemplate(GetResource());
            template.ReceiveTimeout = 10000;

            var streamDLQName = "bindertest.partDLQ.0.dlqPartGrp.dlq";

            var received = template.Receive(streamDLQName);
            Assert.NotNull(received);

            Assert.Equal("bindertest.partDLQ.0.dlqPartGrp-1", received.Headers.ReceivedRoutingKey());
      //      Assert.DoesNotContain(BinderHeaders.PARTITION_HEADER, received.Headers.Select(h => h.Key));

            output.Send(Message.Create(0));
            received = template.Receive(streamDLQName);
            Assert.NotNull(received);
            Assert.Equal("bindertest.partDLQ.0.dlqPartGrp-0", received.Headers.ReceivedRoutingKey());
           // Assert.DoesNotContain(BinderHeaders.PARTITION_HEADER, received.Headers.Select(h => h.Key));

            input0Binding.Unbind();
            input1Binding.Unbind();
            defaultConsumerBinding1.Unbind();
            defaultConsumerBinding2.Unbind();
            outputBinding.Unbind();
        }

        [Fact]
        public void TestAutoBindDLQPartitionedConsumerFirstWithRepublishNoRetry()
        {
            TestAutoBindDLQPartionedConsumerFirstWithRepublishGuts(false);
        }

        [Fact]
        public void TestAutoBindDLQPartitionedConsumerFirstWithRepublishWithRetry()
        {
            TestAutoBindDLQPartionedConsumerFirstWithRepublishGuts(true);
        }

        [Fact]
        public void TestAutoBindDLQPartitionedProducerFirst()
        {
            var binder = GetBinder();
            var properties = CreateProducerOptions() as ExtendedProducerOptions<RabbitProducerOptions>;

            properties.Extension.Prefix = "bindertest.";
            properties.Extension.AutoBindDlq = true;
            properties.RequiredGroups = new string[] { "dlqPartGrp" }.ToList();
            //this.applicationContext.registerBean("pkExtractor", PartitionTestSupport.class, ()-> new PartitionTestSupport());
            binder.ApplicationContext.Register("pkExtractor", new TestPartitionSupport("pkExtractor"));
            properties.PartitionKeyExtractorName = "pkExtractor";
            properties.PartitionSelectorName = "pkExtractor";
            properties.PartitionCount = 2;
            var output = CreateBindableChannel("output", CreateProducerBindingOptions(properties));
            output.ComponentName = "test.output";
            var outputBinding = binder.BindProducer("partDLQ.1", output, properties);

            var consumerProperties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            consumerProperties.Extension.Prefix = "bindertest.";
            consumerProperties.Extension.AutoBindDlq = true;
            consumerProperties.MaxAttempts = 1; // disable retry
            consumerProperties.Partitioned = true;
            consumerProperties.InstanceIndex = 0;
            DirectChannel input0 = CreateBindableChannel("input", CreateConsumerBindingOptions(consumerProperties));
            input0.ComponentName = "test.input0DLQ";
            var input0Binding = binder.BindConsumer("partDLQ.1", "dlqPartGrp", input0, consumerProperties);
            var defaultConsumerBinding1 = binder.BindConsumer("partDLQ.1", "defaultConsumer", new QueueChannel(), consumerProperties);
            consumerProperties.InstanceIndex = 1;
            DirectChannel input1 = CreateBindableChannel("input1", CreateConsumerBindingOptions(consumerProperties));
            input1.ComponentName = "test.input1DLQ";
            var input1Binding = binder.BindConsumer("partDLQ.1", "dlqPartGrp", input1, consumerProperties);
            var defaultConsumerBinding2 = binder.BindConsumer("partDLQ.1", "defaultConsumer", new QueueChannel(), consumerProperties);

            var latch0 = new CountdownEvent(1);
            input0.Subscribe(new TestMessageHandler()
            {
                OnHandleMessage = (message) =>
                 {
                     if (latch0.CurrentCount <= 0)
                     {
                         throw new Exception("dlq");
                     }

                     latch0.Signal();
                 }
            });

            var latch1 = new CountdownEvent(1);
            input1.Subscribe(new TestMessageHandler()
            {
                OnHandleMessage = (message) =>
                {
                    if (latch1.CurrentCount <= 0)
                    {
                        throw new Exception("dlq");
                    }
                    latch1.Signal();
                }
            });

            output.Send(Message.Create(1));
            Assert.True(latch1.Wait(TimeSpan.FromSeconds(10)));

            output.Send(Message.Create(0));
            Assert.True(latch0.Wait(TimeSpan.FromSeconds(10)));

            output.Send(Message.Create(1));

            RabbitTemplate template = new RabbitTemplate(GetResource());
            template.ReceiveTimeout = 10000;

            var streamDLQName = "bindertest.partDLQ.1.dlqPartGrp.dlq";

            var received = template.Receive(streamDLQName);
            Assert.NotNull(received);
            //assertThat(received.getMessageProperties().getReceivedRoutingKey())
            //    .isEqualTo("bindertest.partDLQ.1.dlqPartGrp-1");
            //assertThat(received.getMessageProperties().getHeaders())
            //    .doesNotContainKey(BinderHeaders.PARTITION_HEADER);
            //assertThat(received.getMessageProperties().getReceivedDeliveryMode())
            //    .isEqualTo(MessageDeliveryMode.PERSISTENT);

            output.Send(Message.Create(0));
            received = template.Receive(streamDLQName);
            Assert.NotNull(received);
            //    assertThat(received.getMessageProperties().getReceivedRoutingKey())
            //    .isEqualTo("bindertest.partDLQ.1.dlqPartGrp-0");
            //assertThat(received.getMessageProperties().getHeaders())
            //    .doesNotContainKey(BinderHeaders.PARTITION_HEADER);

            input0Binding.Unbind();
            input1Binding.Unbind();
            defaultConsumerBinding1.Unbind();
            defaultConsumerBinding2.Unbind();
            outputBinding.Unbind();
           
        }

        [Fact]
        public void TestAutoBindDLQwithRepublish()
        {
            this.maxStackTraceSize = RabbitUtils.GetMaxFrame(GetResource()) - 20_000;
            Assert.True(this.maxStackTraceSize > 0);

            var binder = GetBinder();
            var consumerProperties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            consumerProperties.Extension.Prefix = TEST_PREFIX;
            consumerProperties.Extension.AutoBindDlq = true;
            consumerProperties.Extension.RepublishToDlq = true;
            consumerProperties.MaxAttempts = 1; // disable retry
            consumerProperties.Extension.DurableSubscription = true;
            DirectChannel moduleInputChannel = CreateBindableChannel("input", CreateConsumerBindingOptions(consumerProperties));
            moduleInputChannel.ComponentName = "dlqPubTest";
            var exception = BigCause();

            Assert.True(exception.StackTrace.Length > this.maxStackTraceSize);
            var dontRepublish = new AtomicBoolean();
            moduleInputChannel.Subscribe(new TestMessageHandler()
            {
                OnHandleMessage = (m) =>
                {
                    if (dontRepublish.Value)
                    {
                        throw new ImmediateAcknowledgeException("testDontRepublish");
                    }

                    throw exception;
                }
            });

            consumerProperties.Multiplex = true;
            var consumerBinding = binder.BindConsumer("foo.dlqpubtest,foo.dlqpubtest2", "foo", moduleInputChannel, consumerProperties);

            var template = new RabbitTemplate(GetResource());
            template.ConvertAndSend(string.Empty, TEST_PREFIX + "foo.dlqpubtest.foo", "foo");

            template.ReceiveTimeout = 10_000;
            //  Thread.Sleep(1000000);
            var deadLetter = template.Receive(TEST_PREFIX + "foo.dlqpubtest.foo.dlq");
            Assert.NotNull(deadLetter);
            //Assert.Equal("foo", ((byte[])deadLetter.Payload).GetString());
            //Assert.Contains(RepublishMessageRecoverer.X_EXCEPTION_STACKTRACE, deadLetter.Headers);
            ////    assertThat(((LongString) deadLetter.getMessageProperties().getHeaders()
            ////            .get(RepublishMessageRecoverer.X_EXCEPTION_STACKTRACE)).length())
            ////        .isEqualTo(this.maxStackTraceSize);

            //template.ConvertAndSend(string.Empty, TEST_PREFIX + "foo.dlqpubtest2.foo", "bar");

            //deadLetter = template.Receive(TEST_PREFIX + "foo.dlqpubtest2.foo.dlq");
            //Assert.NotNull(deadLetter);
            ////    assertThat(new String(deadLetter.getBody())).isEqualTo("bar");
            ////    assertThat(deadLetter.getMessageProperties().getHeaders())
            ////        .containsKey(("x-exception-stacktrace"));

            //dontRepublish.GetAndSet(true);
            //template.ConvertAndSend("", TEST_PREFIX + "foo.dlqpubtest2.foo", "baz");
            //template.ReceiveTimeout = 500;
            //Assert.Null(template.Receive(TEST_PREFIX + "foo.dlqpubtest2.foo.dlq"));

            consumerBinding.Unbind();
           
        }

        [Fact]
        public void TestBatchingAndCompression()
        {
            var binder = GetBinder();
            var producerProperties = CreateProducerOptions() as ExtendedProducerOptions<RabbitProducerOptions>;
            producerProperties.Extension
                        .DeliveryMode = MessageDeliveryMode.NON_PERSISTENT;
            producerProperties.Extension.BatchingEnabled = true;
            producerProperties.Extension.BatchSize = 2;
            producerProperties.Extension.BatchBufferLimit = 100000;
            producerProperties.Extension.BatchTimeout = 30000;
            producerProperties.Extension.Compress = true;
            producerProperties.RequiredGroups = new string[] { "default" }.ToList();

            DirectChannel output = CreateBindableChannel("output", CreateProducerBindingOptions(producerProperties));
            output.ComponentName = "batchingProducer";
            var producerBinding = binder.BindProducer("batching.0", output, producerProperties);

            //Log logger = spy(TestUtils.getPropertyValue(binder,
            //            "binder.compressingPostProcessor.logger", Log.class));
            //new DirectFieldAccessor(
            //        TestUtils.getPropertyValue(binder, "binder.compressingPostProcessor"))
            //        .PropertyValue = "logger", logger;
            //when(logger.isTraceEnabled()).thenReturn(true);

            //assertThat(TestUtils.getPropertyValue(binder,
            //        "binder.compressingPostProcessor.level")).isEqualTo(Deflater.BEST_SPEED);


            var fooMessage = Message.Create("foo".GetBytes());
            var barMessage = Message.Create("bar".GetBytes());

            output.Send(fooMessage);
            output.Send(barMessage);

            //        Object out = spyOn("batching.0.default").receive(false);
            //    assertThat(out).isInstanceOf(byte[].class);
            //assertThat(new String((byte[]) out))
            //        .isEqualTo("\u0000\u0000\u0000\u0003foo\u0000\u0000\u0000\u0003bar");

            //    ArgumentCaptor<Object> captor = ArgumentCaptor.forClass(Object.class);
            //verify(logger).trace(captor.capture());
            //    assertThat(captor.getValue().toString()).contains(("Compressed 14 to "));

            QueueChannel input = new QueueChannel();
            input.ComponentName = "batchingConsumer";
            var consumerBinding = binder.BindConsumer("batching.0", "test", input, CreateConsumerOptions());

            output.Send(fooMessage);
            output.Send(barMessage);

            var inMessage = (Message<byte[]>)input.Receive(10000);
            Assert.NotNull(inMessage);
            Assert.Equal("foo", inMessage.Payload.GetString());
            inMessage = (Message<byte[]>)input.Receive(10000);

            Assert.NotNull(inMessage);
            Assert.Equal("bar", inMessage.Payload.GetString());
            Assert.Null(inMessage.Headers[RabbitMessageHeaders.DELIVERY_MODE]);

            producerBinding.Unbind();
            consumerBinding.Unbind();
           
        }

        [Fact]
        public void TestInternalHeadersNotPropagated()
        {
            RabbitTestBinder binder = GetBinder();
            var producerProperties = CreateProducerOptions() as ExtendedProducerOptions<RabbitProducerOptions>;
            producerProperties.Extension.DeliveryMode = MessageDeliveryMode.NON_PERSISTENT;

            DirectChannel output = CreateBindableChannel("output", CreateProducerBindingOptions(producerProperties));
            output.ComponentName = "propagate.out";
            var producerBinding = binder.BindProducer("propagate.1", output, producerProperties);

            QueueChannel input = new QueueChannel();
            input.ComponentName = "propagate.in";
            var consumerProperties = CreateConsumerOptions();
            var consumerBinding = binder.BindConsumer("propagate.0", "propagate", input, consumerProperties);

            var logger = LoggerFactory.CreateLogger<RabbitAdmin>();
            var admin = new RabbitAdmin(GetResource(), logger);

            var queue = new Queue("propagate");
            admin.DeclareQueue(new Queue("propagate"));
            admin.DeclareBinding(new RabbitBinding("propagate_binding", "propagate", RabbitBinding.DestinationType.QUEUE, "propagate.1", "#", null));
            var template = new RabbitTemplate(GetResource());

            template.ConvertAndSend("propagate.0.propagate", "foo");
            var message = input.Receive(10_000);
            Assert.NotNull(message);
            output.Send(message);
            var received = template.Receive("propagate", 10_000);
            Assert.NotNull(received);

            Assert.Equal("foo".GetBytes(), received.Payload);
            //Object header = received.getMessageProperties().getHeader(IntegrationMessageHeaderAccessor.SOURCE_DATA);
            //assertThat(header).isNull();
            //header = received.getMessageProperties().getHeader(IntegrationMessageHeaderAccessor.DELIVERY_ATTEMPT);
            //assertThat(header).isNull();

            producerBinding.Unbind();
            consumerBinding.Unbind();
            admin.DeleteQueue("propagate");
           
        }

        /*
         * Test late binding due to broker down; queues with and without DLQs, and partitioned
         * queues.
         * Not working ...
         */
        [Fact]
        public void TestLateBinding()
        {
            var proxy = new RabbitProxy(LoggerFactory.CreateLogger<RabbitProxy>());

            CachingConnectionFactory cf = new CachingConnectionFactory("127.0.0.1", proxy.Port, LoggerFactory);

            var context = RabbitTestBinder.GetApplicationContext();
            var provisioner = new RabbitExchangeQueueProvisioner(cf, new RabbitBindingsOptions(), context, LoggerFactory.CreateLogger<RabbitExchangeQueueProvisioner>());
            var rabbitBinder = new RabbitMessageChannelBinder(context, LoggerFactory.CreateLogger<RabbitMessageChannelBinder>(), cf, new RabbitOptions(), null, new RabbitBindingsOptions(), provisioner);
            RabbitTestBinder binder = new RabbitTestBinder(cf, rabbitBinder, LoggerFactory.CreateLogger<RabbitTestBinder>());
            _testBinder = binder;

            var producerProperties = CreateProducerOptions() as ExtendedProducerOptions<RabbitProducerOptions>;
            producerProperties.Extension.Prefix = "latebinder.";
            producerProperties.Extension.AutoBindDlq = true;
            producerProperties.Extension.Transacted = true;

            var moduleOutputChannel = CreateBindableChannel("output", CreateProducerBindingOptions(producerProperties));
            var late0ProducerBinding = binder.BindProducer("late.0", moduleOutputChannel, producerProperties);

            QueueChannel moduleInputChannel = new QueueChannel();
            var rabbitConsumerProperties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            rabbitConsumerProperties.Extension.Prefix = "latebinder.";
            var late0ConsumerBinding = binder.BindConsumer("late.0", "test", moduleInputChannel, rabbitConsumerProperties);
            producerProperties.PartitionKeyExpression = "payload.equals('0') ? 0 : 1";
            producerProperties.PartitionSelectorExpression = "hashCode()";
            producerProperties.PartitionCount = 2;

            var partOutputChannel = CreateBindableChannel("output", CreateProducerBindingOptions(producerProperties));
            var partlate0ProducerBinding = binder.BindProducer("partlate.0", partOutputChannel, producerProperties);

            var partInputChannel0 = new QueueChannel();
            var partInputChannel1 = new QueueChannel();

            var partLateConsumerProperties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            partLateConsumerProperties.Extension.Prefix = "latebinder.";
            partLateConsumerProperties.Partitioned = true;
            partLateConsumerProperties.InstanceIndex = 0;

            var partlate0Consumer0Binding = binder.BindConsumer("partlate.0", "test", partInputChannel0, partLateConsumerProperties);
            partLateConsumerProperties.InstanceIndex = 1;
            var partlate0Consumer1Binding = binder.BindConsumer("partlate.0", "test", partInputChannel1, partLateConsumerProperties);

            var noDlqProducerProperties = CreateProducerOptions() as ExtendedProducerOptions<RabbitProducerOptions>;
            noDlqProducerProperties.Extension.Prefix = "latebinder.";
            var noDLQOutputChannel = CreateBindableChannel("output", CreateProducerBindingOptions(noDlqProducerProperties));
            var noDlqProducerBinding = binder.BindProducer("lateNoDLQ.0", noDLQOutputChannel, noDlqProducerProperties);

            var noDLQInputChannel = new QueueChannel();
            var noDlqConsumerProperties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            noDlqConsumerProperties.Extension.Prefix = "latebinder.";
            var noDlqConsumerBinding = binder.BindConsumer("lateNoDLQ.0", "test", noDLQInputChannel, noDlqConsumerProperties);

            var outputChannel = CreateBindableChannel("output", CreateProducerBindingOptions(noDlqProducerProperties));
            var pubSubProducerBinding = binder.BindProducer("latePubSub", outputChannel, noDlqProducerProperties);
            var pubSubInputChannel = new QueueChannel();
            noDlqConsumerProperties.Extension.DurableSubscription = false;
            var nonDurableConsumerBinding = binder.BindConsumer("latePubSub", "lategroup", pubSubInputChannel, noDlqConsumerProperties);

            var durablePubSubInputChannel = new QueueChannel();
            noDlqConsumerProperties.Extension.DurableSubscription = true;
            var durableConsumerBinding = binder.BindConsumer("latePubSub", "lateDurableGroup", durablePubSubInputChannel, noDlqConsumerProperties);


            proxy.Start();

            moduleOutputChannel.Send(MessageBuilder.WithPayload("foo")
                    .SetHeader(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN)
                    .Build());

            var message = moduleInputChannel.Receive(20000);
            Assert.NotNull(message);
            Assert.NotNull(message.Payload);

            noDLQOutputChannel.Send(MessageBuilder.WithPayload("bar")
                    .SetHeader(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN)
                    .Build());

            message = noDLQInputChannel.Receive(10000);
            Assert.NotNull(message);
            Assert.Equal("bar".GetBytes(), message.Payload);

            outputChannel.Send(MessageBuilder.WithPayload("baz")
                        .SetHeader(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN)
                        .Build());
            message = pubSubInputChannel.Receive(10000);
            Assert.NotNull(message);
            Assert.Equal("baz".GetBytes(), message.Payload);
            message = durablePubSubInputChannel.Receive(10000);
            Assert.NotNull(message);
            Assert.Equal("baz".GetBytes(), message.Payload);

            // TODO: SPEL

            //partOutputChannel.Send(MessageBuilder.WithPayload("0")
            //        .SetHeader(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN)
            //        .Build());
            //partOutputChannel.Send(MessageBuilder.WithPayload("1")
            //        .SetHeader(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN)
            //        .Build());

            //message = partInputChannel0.Receive(10000);
            //Assert.NotNull(message);

            //Assert.Equal("0".GetBytes(), message.Payload);
            //message = partInputChannel1.Receive(10000);
            //Assert.NotNull(message);
            //Assert.Equal("1".GetBytes(), message.Payload);

            // late0ProducerBinding.Unbind();
            //  late0ConsumerBinding.Unbind();
            //partlate0ProducerBinding.Unbind();
            //partlate0Consumer0Binding.Unbind();
            //partlate0Consumer1Binding.Unbind();
            //noDlqProducerBinding.Unbind();
            //noDlqConsumerBinding.Unbind();
            //pubSubProducerBinding.Unbind();
            //nonDurableConsumerBinding.Unbind();
            //durableConsumerBinding.Unbind();


            Cleanup();

            proxy.Stop();
            cf.Destroy();

            GetResource().Destroy();
        }

        [Fact]
        public async void TestBadUserDeclarationsFatal()
        {
            var binder = GetBinder();
            var context = binder.ApplicationContext;
            context.Register("testBadUserDeclarationsFatal", new Queue("testBadUserDeclarationsFatal", false));
            context.Register("binder", binder);

            var channelBinder = binder.Binder;
            var provisioner = GetPropertyValue<RabbitExchangeQueueProvisioner>(channelBinder, "ProvisioningProvider");

            context.Register("provisioner", provisioner);

            var admin = new RabbitAdmin(GetResource(), LoggerFactory.CreateLogger<RabbitAdmin>());
            admin.DeclareQueue(new Queue("testBadUserDeclarationsFatal"));

            // reset the connection and configure the "user" admin to auto declare queues...
            GetResource().ResetConnection();

            context.Register("rabbitAdmin", admin);

            // the mis-configured queue should be fatal

            IBinding binding = null;
            try
            {
                await Assert.ThrowsAsync<BinderException>(() =>
                {
                    binding = binder.BindConsumer("input", "baddecls", this.CreateBindableChannel("input", GetDefaultBindingOptions()), CreateConsumerOptions());
                    throw new Exception("Expected exception");
                });
            }
            finally
            {
                admin.DeleteQueue("testBadUserDeclarationsFatal");
                if (binding != null)
                {
                   await binding.Unbind();
                }
            }
        }

        // TODO: Pending EL
        //[Fact]
        //public void TestRoutingKeyExpression()
        //{
        //    var binder = GetBinder();
        //    var producerProperties = CreateProducerOptions() as ExtendedProducerOptions<RabbitProducerOptions>;
        //    producerProperties.Extension.RoutingKeyExpression = "payload.field";

        //    var output = CreateBindableChannel("output", CreateProducerBindingOptions(producerProperties));
        //    output.ComponentName = "rkeProducer";
        //    var producerBinding = binder.BindProducer("rke", output, producerProperties);

        //    RabbitAdmin admin = new RabbitAdmin(GetResource());
        //    Queue queue = new AnonymousQueue();
        //    TopicExchange exchange = new TopicExchange("rke");
        //    var binding = BindingBuilder.Bind(queue).To(exchange).With("rkeTest");
        //    admin.DeclareQueue(queue);
        //    admin.DeclareBinding(binding);

        //    output.AddInterceptor(new TestChannelInterceptor()
        //    {
        //        PresendHandler = (message, channel) =>
        //        {
        //            Assert.Equal("rkeTest", message.Headers[RabbitExpressionEvaluatingInterceptor.ROUTING_KEY_HEADER]);
        //            return message;
        //        }
        //    });

        //    output.Send(Message.Create(new Poco("rkeTest")));

        //    //    var out = spyOn(queue.getName()).receive(false);
        //    //assertThat(out).isInstanceOf(byte[].class);
        //    //assertThat(new String((byte[]) out, StandardCharsets.UTF_8))
        //    //        .isEqualTo("{\"field\":\"rkeTest\"}");

        //    producerBinding.Unbind();
        //}

        //// TODO: Pending EL
        //[Fact]
        //public void TestRoutingKeyExpressionPartitionedAndDelay()
        //{
        //    var binder = GetBinder();
        //    var producerProperties = CreateProducerOptions() as ExtendedProducerOptions<RabbitProducerOptions>;
        //    producerProperties.Extension.RoutingKeyExpression = "#root.getPayload().field";
        //    // requires delayed message exchange plugin; tested locally
        //    // producerProperties.Extension.DelayedExchange = true;
        //    producerProperties.Extension.DelayExpression = "1000";
        //    producerProperties.PartitionKeyExpression = "0";

        //    DirectChannel output = CreateBindableChannel("output", CreateProducerBindingOptions(producerProperties));
        //    output.ComponentName = "rkeProducer";
        //    var producerBinding = binder.BindProducer("rkep", output, producerProperties);

        //    var admin = new RabbitAdmin(GetResource());
        //    Queue queue = new AnonymousQueue();
        //    TopicExchange exchange = new TopicExchange("rkep");
        //    var binding = BindingBuilder.Bind(queue).To(exchange).With("rkepTest-0");
        //    admin.DeclareQueue(queue);
        //    admin.DeclareBinding(binding);

        //    output.AddInterceptor(new TestChannelInterceptor()
        //    {
        //        PresendHandler = (message, channel) =>
        //            {
        //                Assert.Equal("rkepTest", message.Headers[RabbitExpressionEvaluatingInterceptor.ROUTING_KEY_HEADER]);

        //                Assert.Equal(1000, message.Headers[RabbitExpressionEvaluatingInterceptor.DELAY_HEADER]);
        //                return message;
        //            }
        //    });

        //    output.Send(Message.Create(new Poco("rkepTest")));

        //    //    var out = spyOn(queue.getName()).receive(false);
        //    //assertThat(out).isInstanceOf(byte[].class);
        //    //assertThat(new String((byte[]) out, StandardCharsets.UTF_8))
        //    //        .isEqualTo("{\"field\":\"rkepTest\"}");

        //    producerBinding.Unbind();
        //}

        [Fact]
        public void TestPolledConsumer()
        {
            var binder = GetBinder();
            var messageConverter = new CompositeMessageConverterFactory().MessageConverterForAllRegistered;
            var inboundBindTarget = new DefaultPollableMessageSource(binder.ApplicationContext, messageConverter);
            var binding = binder.BindPollableConsumer("pollable", "group", inboundBindTarget, CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>);
            var template = new RabbitTemplate(GetResource());
            template.ConvertAndSend("pollable.group", "testPollable");

            var polled = inboundBindTarget.Poll(new TestMessageHandler()
            {
                OnHandleMessage = (m) =>
                  {
                      Assert.Equal("testPollable", m.Payload);
                  }
            });

            int n = 0;
            while (n++ < 100 && !polled)
            {
                polled = inboundBindTarget.Poll(new TestMessageHandler()
                {
                    OnHandleMessage = (m) =>
                      {
                          Assert.Equal("testPollable", m.Payload);
                      }
                });
            }

            Assert.True(polled);
            binding.Unbind();
        }

        [Fact]
        public void TestPolledConsumerRequeue()
        {
            var binder = GetBinder();
            var messageConverter = new CompositeMessageConverterFactory().MessageConverterForAllRegistered;
            var inboundBindTarget = new DefaultPollableMessageSource(binder.ApplicationContext, messageConverter);
            var properties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            var binding = binder.BindPollableConsumer("pollableRequeue", "group", inboundBindTarget, properties);
            var template = new RabbitTemplate(GetResource());
            template.ConvertAndSend("pollableRequeue.group", "testPollable");
            try
            {
                bool polled = false;
                int n = 0;
                while (n++ < 100 && !polled)
                {
                    polled = inboundBindTarget.Poll(new TestMessageHandler()
                    {
                        OnHandleMessage = (m) =>
                        {
                            Assert.Equal("testPollable", m.Payload);
                            throw new RequeueCurrentMessageException();
                        }
                    });
                }
            }
            catch (MessageHandlingException e)
            {
                Assert.IsAssignableFrom<RequeueCurrentMessageException>(e.InnerException);
            }

            var isPolled = inboundBindTarget.Poll(new TestMessageHandler()
            {
                OnHandleMessage = (m) =>
               {
                   Assert.Equal("testPollable", m.Payload);
               }
            });

            Assert.True(isPolled);
            binding.Unbind();
        }

        [Fact]
        public void TestPolledConsumerWithDlq()
        {
            var binder = GetBinder();
            var messageConverter = new CompositeMessageConverterFactory().MessageConverterForAllRegistered;
            var inboundBindTarget = new DefaultPollableMessageSource(binder.ApplicationContext, messageConverter);
            var properties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            properties.MaxAttempts = 2;
            properties.BackOffInitialInterval = 0;
            properties.Extension.AutoBindDlq = true;
            var binding = binder.BindPollableConsumer("pollableDlq", "group", inboundBindTarget, properties);
            var template = new RabbitTemplate(GetResource());
            template.ConvertAndSend("pollableDlq.group", "testPollable");
            try
            {
                int n = 0;
                while (n++ < 100)
                {
                    inboundBindTarget.Poll(new TestMessageHandler()
                    {
                        OnHandleMessage = (m) =>
                        {
                            throw new Exception("test DLQ");
                        }
                    });
                    Thread.Sleep(100);
                }
            }
            catch (MessageHandlingException e)
            {
                Assert.Equal("test DLQ", e.InnerException.Message);
            }

            var deadLetter = template.Receive("pollableDlq.group.dlq", 10_000);
            Assert.NotNull(deadLetter);
            binding.Unbind();
        }

        [Fact]
        public void TestPolledConsumerWithDlqNoRetry()
        {
            var binder = GetBinder();
            var messageConverter = new CompositeMessageConverterFactory().MessageConverterForAllRegistered;
            var inboundBindTarget = new DefaultPollableMessageSource(binder.ApplicationContext, messageConverter);
            var properties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            properties.MaxAttempts = 1;
            // properties.Extension.RequeueRejected = true; // loops, correctly
            properties.Extension.AutoBindDlq = true;
            var binding = binder.BindPollableConsumer("pollableDlqNoRetry", "group", inboundBindTarget, properties);
            RabbitTemplate template = new RabbitTemplate(GetResource());

            template.ConvertAndSend("pollableDlqNoRetry.group", "testPollable");
            try
            {
                int n = 0;
                while (n++ < 100)
                {
                    inboundBindTarget.Poll(new TestMessageHandler()
                    {
                        OnHandleMessage = (m) =>
                        {
                            throw new Exception("test DLQ");
                        }
                    });

                    Thread.Sleep(100);
                }
            }
            catch (MessageHandlingException e)
            {
               Assert.Equal("test DLQ", e.Message);
            }

            var deadLetter = template.Receive("pollableDlqNoRetry.group.dlq", 10_000);
            Assert.NotNull(deadLetter);
            binding.Unbind();
        }

        [Fact]
        public void TestPolledConsumerWithDlqRePub()
        {
            var binder = GetBinder();
            var messageConverter = new CompositeMessageConverterFactory().MessageConverterForAllRegistered;
            var inboundBindTarget = new DefaultPollableMessageSource(binder.ApplicationContext, messageConverter);
            var properties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            properties.MaxAttempts = 2;
            properties.BackOffInitialInterval = 0;
            properties.Extension.AutoBindDlq = true;
            properties.Extension.RepublishToDlq = true;
            var binding = binder.BindPollableConsumer("pollableDlqRePub", "group", inboundBindTarget, properties);
            RabbitTemplate template = new RabbitTemplate(GetResource());
            template.ConvertAndSend("pollableDlqRePub.group", "testPollable");
            var polled = false;
            int n = 0;
            while (n++ < 100 && !polled)
            {
                Thread.Sleep(100);
                polled = inboundBindTarget.Poll(new TestMessageHandler()
                {
                    OnHandleMessage = (m) =>
                    {
                        throw new Exception("test DLQ");
                    }
                });
            }

            Assert.True(polled);
            var deadLetter = template.Receive("pollableDlqRePub.group.dlq", 10_000);
            Assert.NotNull(deadLetter);
            binding.Unbind();
        }

        private void TestAutoBindDLQPartionedConsumerFirstWithRepublishGuts(bool withRetry)
        {
            var logger = new XunitLogger(Output);
            var binder = GetBinder();

            // Setup binder configuration to mimic autoconfig
            RegisterGlobalErrorChannel(binder);

            var properties = CreateConsumerOptions() as ExtendedConsumerOptions<RabbitConsumerOptions>;
            properties.Extension.Prefix = "bindertest.";
            properties.Extension.AutoBindDlq = true;
            properties.Extension.RepublishToDlq = true;
            properties.Extension.RepublishDeliveryMode = MessageDeliveryMode.NON_PERSISTENT;
            properties.MaxAttempts = withRetry ? 2 : 1;
            properties.Partitioned = true;
            properties.InstanceIndex = 0;
            var input0 = CreateBindableChannel("input", CreateConsumerBindingOptions(properties));
            input0.ComponentName = "test.input0DLQ";
            var input0Binding = binder.BindConsumer("partPubDLQ.0", "dlqPartGrp", input0, properties);
            var defaultConsumerBinding1 = binder.BindConsumer("partPubDLQ.0", "default", new QueueChannel(LoggerFactory.CreateLogger<QueueChannel>()), properties);
            properties.InstanceIndex = 1;

            var input1 = CreateBindableChannel("input1", CreateConsumerBindingOptions(properties));
            input1.ComponentName = "test.input1DLQ";
            var input1Binding = binder.BindConsumer("partPubDLQ.0", "dlqPartGrp", input1, properties);
            var defaultConsumerBinding2 = binder.BindConsumer("partPubDLQ.0", "default", new QueueChannel(), properties);

            var producerProperties = CreateProducerOptions() as ExtendedProducerOptions<RabbitProducerOptions>;
            producerProperties.Extension.Prefix = "bindertest.";
            producerProperties.Extension.AutoBindDlq = true;

            binder.ApplicationContext.Register("pkExtractor", new TestPartitionSupport("pkExtractor"));
            binder.ApplicationContext.Register("pkSelector", new TestPartitionSupport("pkSelector"));

            producerProperties.PartitionKeyExtractorName = "pkExtractor";
            producerProperties.PartitionSelectorName = "pkSelector";
            producerProperties.PartitionCount = 2;
            var bindingProperties = CreateProducerBindingOptions(producerProperties);

            DirectChannel output = CreateBindableChannel("output", bindingProperties);
            output.ComponentName = "test.output";
            var outputBinding = binder.BindProducer("partPubDLQ.0", output, producerProperties);

            var latch0 = new CountdownEvent(1);
            input0.Subscribe(new TestMessageHandler()
            {
                OnHandleMessage = (message) =>
                {
                    if (latch0.CurrentCount <= 0)
                    {
                        throw new Exception("dlq");
                    }

                    latch0.Signal();
                }
            });

            var latch1 = new CountdownEvent(1);
            input1.Subscribe(new TestMessageHandler()
            {
                OnHandleMessage = (message) =>
                {
                    if (latch1.CurrentCount <= 0)
                    {
                        throw new Exception("dlq");
                    }

                    latch1.Signal();
                }
            });

            var context = binder.ApplicationContext;
            var boundErrorChannel = context.GetService<ISubscribableChannel>("bindertest.partPubDLQ.0.dlqPartGrp-0.errors");
            var globalErrorChannel = context.GetService<ISubscribableChannel>("errorChannel");

            var boundErrorChannelMessage = new AtomicReference<IMessage>();
            var globalErrorChannelMessage = new AtomicReference<IMessage>();
            var hasRecovererInCallStack = new AtomicBoolean(!withRetry);

            boundErrorChannel.Subscribe(new TestMessageHandler()
            {
                OnHandleMessage = (message) =>
                {
                    boundErrorChannelMessage.GetAndSet(message);
                    var stackTrace = new Exception().StackTrace.ToString();
                    hasRecovererInCallStack.GetAndSet(stackTrace.Contains("ErrorMessageSendingRecoverer"));
                }
            });

            globalErrorChannel.Subscribe(new TestMessageHandler()
            {
                OnHandleMessage = (message) =>
                {
                    globalErrorChannelMessage.GetAndSet(message);
                }
            });

            output.Send(Message.Create(1));
            Assert.True(latch1.Wait(TimeSpan.FromSeconds(10)));

            output.Send(Message.Create(0));
            Assert.True(latch0.Wait(TimeSpan.FromSeconds(10)));

            output.Send(Message.Create(1));

            var template = new RabbitTemplate(GetResource());

            template.ReceiveTimeout = 10000;

            var streamDLQName = "bindertest.partPubDLQ.0.dlqPartGrp.dlq";

            var received = template.Receive(streamDLQName);
            Assert.NotNull(received);
            //assertThat(
            //        received.getMessageProperties().getHeaders().get("x-original-routingKey"))
            //    .isEqualTo("partPubDLQ.0-1");
            //assertThat(received.getMessageProperties().getHeaders())
            //    .doesNotContainKey(BinderHeaders.PARTITION_HEADER);
            //assertThat(received.getMessageProperties().getReceivedDeliveryMode())
            //    .isEqualTo(MessageDeliveryMode.NON_PERSISTENT);

            output.Send(Message.Create(0));
            received = template.Receive(streamDLQName);
            Assert.NotNull(received);
            //    assertThat(
            //        received.getMessageProperties().getHeaders().get("x-original-routingKey"))
            //    .isEqualTo("partPubDLQ.0-0");
            //assertThat(received.getMessageProperties().getHeaders())
            //    .doesNotContainKey(BinderHeaders.PARTITION_HEADER);

            //// verify we got a message on the dedicated error channel and the global (via
            //// bridge)
            ///
            Thread.Sleep(2000);
            // Assert.NotNull(boundErrorChannelMessage.Value);

            //Assert.Equal(withRetry, hasRecovererInCallStack.Value);
            Assert.NotNull(globalErrorChannelMessage.Value);

            input0Binding.Unbind();
            input1Binding.Unbind();
            defaultConsumerBinding1.Unbind();
            defaultConsumerBinding2.Unbind();
            outputBinding.Unbind();
           
        }

        //TODO: Replace with extension method
        private void RegisterGlobalErrorChannel(RabbitTestBinder binder)
        {
            var appcontext = binder.ApplicationContext;
            var errorChannel = new BinderErrorChannel(appcontext, IntegrationContextUtils.ERROR_CHANNEL_BEAN_NAME, LoggerFactory.CreateLogger<BinderErrorChannel>());
            appcontext.Register(IntegrationContextUtils.ERROR_CHANNEL_BEAN_NAME, errorChannel);
        }

        private BindingOptions GetDefaultBindingOptions()
        {
            return new BindingOptions() { ContentType = BindingOptions.DEFAULT_CONTENT_TYPE.ToString() };
        }
    }
}
