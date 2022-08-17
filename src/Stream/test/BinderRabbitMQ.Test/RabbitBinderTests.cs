// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.ExceptionServices;
using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Steeltoe.Common;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Common.Retry;
using Steeltoe.Common.Util;
using Steeltoe.Integration;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Rabbit.Inbound;
using Steeltoe.Integration.Rabbit.Outbound;
using Steeltoe.Integration.Rabbit.Support;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Retry;
using Steeltoe.Messaging.RabbitMQ.Support.PostProcessor;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Binder.Rabbit.Config;
using Steeltoe.Stream.Binder.Rabbit.Provisioning;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Converter;
using Steeltoe.Stream.Provisioning;
using Xunit;
using Xunit.Abstractions;
using static Steeltoe.Messaging.RabbitMQ.Connection.CachingConnectionFactory;
using ExchangeType = Steeltoe.Messaging.RabbitMQ.Config.ExchangeType;
using Message = Steeltoe.Messaging.Message;
using Queue = Steeltoe.Messaging.RabbitMQ.Config.Queue;
using RabbitBinding = Steeltoe.Messaging.RabbitMQ.Config.Binding;

namespace Steeltoe.Stream.Binder.Rabbit;

[Trait("Category", "Integration")]
public sealed class RabbitBinderTests : RabbitBinderTestBase
{
    public RabbitBinderTests(ITestOutputHelper output)
        : base(output)
    {
    }

    [Fact]
    public void TestSendAndReceiveBad()
    {
        var bindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(bindingsOptions);
        DirectChannel moduleOutputChannel = CreateBindableChannel("output", GetDefaultBindingOptions());
        DirectChannel moduleInputChannel = CreateBindableChannel("input", GetDefaultBindingOptions());
        IBinding producerBinding = binder.BindProducer("bad.0", moduleOutputChannel, GetProducerOptions("output", bindingsOptions));

        var endpoint = GetFieldValue<RabbitOutboundEndpoint>(producerBinding, "Lifecycle");

        Assert.True(endpoint.HeadersMappedLast);
        Assert.Contains("PassThrough", endpoint.Template.MessageConverter.GetType().Name);

        ConsumerOptions consumerProps = GetConsumerOptions("input", bindingsOptions);
        RabbitConsumerOptions rabbitConsumerOptions = bindingsOptions.GetRabbitConsumerOptions("input");

        rabbitConsumerOptions.ContainerType = ContainerType.Direct;

        IBinding consumerBinding = binder.BindConsumer("bad.0", "test", moduleInputChannel, consumerProps);

        var inbound = GetFieldValue<RabbitInboundChannelAdapter>(consumerBinding, "Lifecycle");
        Assert.Contains("PassThrough", inbound.MessageConverter.GetType().Name);
        var container = GetPropertyValue<DirectMessageListenerContainer>(inbound, "MessageListenerContainer");
        Assert.NotNull(container);

        IMessage message = MessageBuilder.WithPayload("bad".GetBytes()).SetHeader(MessageHeaders.ContentType, "foo/bar").Build();

        var latch = new CountdownEvent(3);

        moduleInputChannel.Subscribe(new TestMessageHandler
        {
            OnHandleMessage = _ =>
            {
                latch.Signal();
                throw new Exception();
            }
        });

        moduleOutputChannel.Send(message);

        Assert.True(latch.Wait(TimeSpan.FromSeconds(10)));

        producerBinding.UnbindAsync();
        consumerBinding.UnbindAsync();
    }

    [Fact]
    public void TestProducerErrorChannel()
    {
        CachingConnectionFactory ccf = GetResource();
        ccf.IsPublisherConfirms = true;
        ccf.PublisherConfirmType = ConfirmType.Correlated;
        ccf.ResetConnection();
        var bindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(bindingsOptions);

        RegisterGlobalErrorChannel(binder);

        DirectChannel moduleOutputChannel = CreateBindableChannel("output", GetDefaultBindingOptions());
        ProducerOptions producerOptions = GetProducerOptions("output", bindingsOptions);
        producerOptions.ErrorChannelEnabled = true;

        IBinding producerBinding = binder.BindProducer("ec.0", moduleOutputChannel, producerOptions);

        IMessage message = MessageBuilder.WithPayload("bad".GetBytes()).SetHeader(MessageHeaders.ContentType, "foo/bar").Build();

        var ec = binder.ApplicationContext.GetService<PublishSubscribeChannel>("ec.0.errors");
        Assert.NotNull(ec);
        var errorMessage = new AtomicReference<IMessage>();

        var latch = new CountdownEvent(2);

        ec.Subscribe(new TestMessageHandler
        {
            OnHandleMessage = message =>
            {
                errorMessage.GetAndSet(message);
                latch.Signal();
            }
        });

        var globalEc = binder.ApplicationContext.GetService<ISubscribableChannel>(IntegrationContextUtils.ErrorChannelBeanName);

        globalEc.Subscribe(new TestMessageHandler
        {
            OnHandleMessage = _ =>
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

        var endpoint = ExtractEndpoint(producerBinding) as RabbitOutboundEndpoint;
        Assert.NotNull(endpoint);
        var expression = GetPropertyValue<SpelExpression>(endpoint, "ConfirmCorrelationExpression");
        Assert.NotNull(expression);
        Assert.Equal("#root", GetPropertyValue<string>(expression, "ExpressionString"));
        var template = new RabbitTemplate();
        var accessor = new WrapperAccessor(null, template);
        CorrelationData correlationData = accessor.GetWrapper(message);

        latch.Reset(2);
        endpoint.Confirm(correlationData, false, "Mock Nack");

        Assert.IsAssignableFrom<ErrorMessage>(errorMessage.Value);

        Assert.IsAssignableFrom<NackedRabbitMessageException>(errorMessage.Value.Payload);
        var nack = errorMessage.Value.Payload as NackedRabbitMessageException;

        Assert.Equal("Mock Nack", nack.NackReason);
        Assert.Equal(message, nack.CorrelationData);
        Assert.Equal(message, nack.FailedMessage);
        producerBinding.UnbindAsync();
    }

    [Fact]
    public void TestProducerAckChannel()
    {
        var bindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(bindingsOptions);
        CachingConnectionFactory ccf = GetResource();
        ccf.IsPublisherReturns = true;
        ccf.PublisherConfirmType = ConfirmType.Correlated;
        ccf.ResetConnection();

        DirectChannel moduleOutputChannel = CreateBindableChannel("output", GetDefaultBindingOptions());
        ProducerOptions producerProps = GetProducerOptions("output", bindingsOptions);
        producerProps.ErrorChannelEnabled = true;

        RabbitProducerOptions rabbitProducerOptions = bindingsOptions.GetRabbitProducerOptions("output");
        rabbitProducerOptions.ConfirmAckChannel = "acksChannel";

        IBinding producerBinding = binder.BindProducer("acks.0", moduleOutputChannel, producerProps);
        byte[] messageBytes = "acksMessage".GetBytes();
        IMessage message = MessageBuilder.WithPayload(messageBytes).Build();

        var confirm = new AtomicReference<IMessage>();
        var confirmLatch = new CountdownEvent(1);

        binder.ApplicationContext.GetService<DirectChannel>("acksChannel").Subscribe(new TestMessageHandler
        {
            OnHandleMessage = m =>
            {
                confirm.GetAndSet(m);
                confirmLatch.Signal();
            }
        });

        moduleOutputChannel.Send(message);
        Assert.True(confirmLatch.Wait(TimeSpan.FromSeconds(10000)));
        Assert.Equal(messageBytes, confirm.Value.Payload);
        producerBinding.UnbindAsync();
    }

    [Fact]
    public void TestProducerConfirmHeader()
    {
        RabbitTestBinder binder = GetBinder();

        CachingConnectionFactory ccf = GetResource();
        ccf.IsPublisherReturns = true;
        ccf.PublisherConfirmType = ConfirmType.Correlated;
        ccf.ResetConnection();

        DirectChannel moduleOutputChannel = CreateBindableChannel("output", GetDefaultBindingOptions());
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        var rabbitBindingOptions = new RabbitBindingOptions();

        ProducerOptions producerProps = GetProducerOptions("output", rabbitBindingsOptions, rabbitBindingOptions);
        rabbitBindingOptions.Producer.UseConfirmHeader = true;
        IBinding producerBinding = binder.BindProducer("confirms.0", moduleOutputChannel, producerProps);

        var correlation = new CorrelationData("testConfirm");

        IMessage message = MessageBuilder.WithPayload("confirmsMessage".GetBytes()).SetHeader(RabbitMessageHeaders.PublishConfirmCorrelation, correlation)
            .Build();

        moduleOutputChannel.Send(message);
        CorrelationData.Confirm confirm = correlation.Future.Result;
        Assert.True(confirm.Ack);

        // Assert.NotNull(correlation.ReturnedMessage); Deprecated in Spring
        producerBinding.UnbindAsync();
    }

    [Fact]
    public void TestConsumerProperties()
    {
        var rabbitConsumerOptions = new RabbitConsumerOptions
        {
            RequeueRejected = true,
            Transacted = true,
            Exclusive = true,
            MissingQueuesFatal = true,
            FailedDeclarationRetryInterval = 1500L,
            QueueDeclarationRetries = 23
        };

        var bindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(bindingsOptions);

        ConsumerOptions properties = GetConsumerOptions("input", bindingsOptions, rabbitConsumerOptions);

        IBinding consumerBinding = binder.BindConsumer("props.0", null, CreateBindableChannel("input", GetDefaultBindingOptions()), properties);

        var endpoint = ExtractEndpoint(consumerBinding) as RabbitInboundChannelAdapter;
        Assert.NotNull(endpoint);
        var container = GetPropertyValue<DirectMessageListenerContainer>(endpoint, "MessageListenerContainer");
        Assert.NotNull(container);
        Assert.Equal(AcknowledgeMode.Auto, container.AcknowledgeMode);
        Assert.StartsWith(rabbitConsumerOptions.Prefix, container.GetQueueNames()[0]);
        Assert.True(container.Exclusive);
        Assert.True(container.IsChannelTransacted);
        Assert.True(container.Exclusive);
        Assert.True(container.DefaultRequeueRejected);
        Assert.Equal(1, container.PrefetchCount);
        Assert.True(container.MissingQueuesFatal);
        Assert.Equal(1500L, container.FailedDeclarationRetryInterval);

        RetryTemplate retry = endpoint.RetryTemplate;
        Assert.NotNull(retry);
        Assert.Equal(3, GetFieldValue<int>(retry, "_maxAttempts"));
        Assert.Equal(1000, GetFieldValue<int>(retry, "_backOffInitialInterval"));
        Assert.Equal(10000, GetFieldValue<int>(retry, "_backOffMaxInterval"));
        Assert.Equal(2.0, GetFieldValue<double>(retry, "_backOffMultiplier"));
        consumerBinding.UnbindAsync();
        Assert.False(endpoint.IsRunning);

        bindingsOptions.Bindings.Remove("input");

        properties = GetConsumerOptions("input", bindingsOptions);
        rabbitConsumerOptions = bindingsOptions.GetRabbitConsumerOptions("input");
        rabbitConsumerOptions.AcknowledgeMode = AcknowledgeMode.None;
        properties.BackOffInitialInterval = 2000;
        properties.BackOffMaxInterval = 20000;
        properties.BackOffMultiplier = 5.0;
        properties.Concurrency = 2;
        properties.MaxAttempts = 23;

        rabbitConsumerOptions.MaxConcurrency = 3;
        rabbitConsumerOptions.Prefix = "foo.";
        rabbitConsumerOptions.Prefetch = 20;

        rabbitConsumerOptions.HeaderPatterns = new[]
        {
            "foo"
        }.ToList();

        rabbitConsumerOptions.BatchSize = 10;
        RabbitCommonOptions.QuorumConfig quorum = rabbitConsumerOptions.Quorum;
        quorum.Enabled = true;
        quorum.DeliveryLimit = 10;
        quorum.InitialQuorumSize = 1;
        properties.InstanceIndex = 0;
        consumerBinding = binder.BindConsumer("props.0", "test", CreateBindableChannel("input", GetDefaultBindingOptions()), properties);

        endpoint = ExtractEndpoint(consumerBinding) as RabbitInboundChannelAdapter;
        container = VerifyContainer(endpoint);

        Assert.Equal("foo.props.0.test", container.GetQueueNames()[0]);

        consumerBinding.UnbindAsync();
        Assert.False(endpoint.IsRunning);
    }

    [Fact]
    public void TestMultiplexOnPartitionedConsumer()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        ConsumerOptions consumerProperties = GetConsumerOptions(string.Empty, rabbitBindingsOptions);
        RabbitProxy proxy = null;

        try
        {
            proxy = new RabbitProxy(LoggerFactory.CreateLogger<RabbitProxy>());
            using var ccf = new CachingConnectionFactory("localhost", proxy.Port);

            var bindingsOptionsMonitor = new TestOptionsMonitor<RabbitBindingsOptions>(rabbitBindingsOptions);

            var rabbitExchangeQueueProvisioner = new RabbitExchangeQueueProvisioner(ccf, bindingsOptionsMonitor,
                GetBinder(rabbitBindingsOptions).ApplicationContext, LoggerFactory.CreateLogger<RabbitExchangeQueueProvisioner>());

            consumerProperties.Multiplex = true;
            consumerProperties.Partitioned = true;

            consumerProperties.InstanceIndexList = new[]
            {
                1,
                2,
                3
            }.ToList();

            IConsumerDestination consumerDestination = rabbitExchangeQueueProvisioner.ProvisionConsumerDestination("foo", "boo", consumerProperties);
            Assert.Equal("foo.boo-1,foo.boo-2,foo.boo-3", consumerDestination.Name);
        }
        finally
        {
            proxy?.Stop();
        }
    }

    [Fact]
    public void TestMultiplexOnPartitionedConsumerWithMultipleDestinations()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        ConsumerOptions consumerProperties = GetConsumerOptions(string.Empty, rabbitBindingsOptions);
        RabbitProxy proxy = null;

        try
        {
            proxy = new RabbitProxy(LoggerFactory.CreateLogger<RabbitProxy>());
            using var ccf = new CachingConnectionFactory("localhost", proxy.Port);
            var bindingsOptionsMonitor = new TestOptionsMonitor<RabbitBindingsOptions>(rabbitBindingsOptions);

            var rabbitExchangeQueueProvisioner = new RabbitExchangeQueueProvisioner(ccf, bindingsOptionsMonitor,
                GetBinder(rabbitBindingsOptions).ApplicationContext, LoggerFactory.CreateLogger<RabbitExchangeQueueProvisioner>());

            consumerProperties.Multiplex = true;
            consumerProperties.Partitioned = true;

            consumerProperties.InstanceIndexList = new[]
            {
                1,
                2,
                3
            }.ToList();

            IConsumerDestination consumerDestination = rabbitExchangeQueueProvisioner.ProvisionConsumerDestination("foo,qaa", "boo", consumerProperties);

            Assert.Equal("foo.boo-1,foo.boo-2,foo.boo-3,qaa.boo-1,qaa.boo-2,qaa.boo-3", consumerDestination.Name);
        }
        finally
        {
            proxy?.Stop();
        }
    }

    [Fact]
    public async Task TestConsumerPropertiesWithUserInfrastructureNoBind()
    {
        ILogger<RabbitAdmin> logger = LoggerFactory.CreateLogger<RabbitAdmin>();
        var admin = new RabbitAdmin(RabbitTestBinder.GetApplicationContext(), GetResource(), logger);
        var queue = new Queue("propsUser1.infra");
        admin.DeclareQueue(queue);

        var exchange = new DirectExchange("propsUser1");
        admin.DeclareExchange(exchange);
        admin.DeclareBinding(BindingBuilder.Bind(queue).To(exchange).With("foo"));

        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ConsumerOptions properties = GetConsumerOptions("input", rabbitBindingsOptions);
        RabbitConsumerOptions rabbitConsumerOptions = rabbitBindingsOptions.GetRabbitConsumerOptions("input");
        rabbitConsumerOptions.DeclareExchange = false;
        rabbitConsumerOptions.BindQueue = false;

        IBinding consumerBinding = binder.BindConsumer("propsUser1", "infra", CreateBindableChannel("input", GetDefaultBindingOptions()), properties);

        ILifecycle endpoint = ExtractEndpoint(consumerBinding);
        var container = GetPropertyValue<DirectMessageListenerContainer>(endpoint, "MessageListenerContainer");

        Assert.False(container.MissingQueuesFatal);
        Assert.True(container.IsRunning);

        await consumerBinding.UnbindAsync();

        Assert.False(container.IsRunning);

        var client = new HttpClient();
        const string scheme = "http://";
        const string vhost = "%2F";
        byte[] byteArray = "guest:guest".GetBytes();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

        HttpResponseMessage response =
            await client.GetAsync($"{scheme}guest:guest@localhost:15672/api/exchanges/{vhost}/{exchange.ExchangeName}/bindings/source");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string jsonResult = await response.Content.ReadAsStringAsync();
        var foo = JsonConvert.DeserializeObject<List<ExpandoObject>>(jsonResult, new ExpandoObjectConverter());

        Assert.Single(foo);
    }

    [Fact]
    public void TestAnonWithBuiltInExchange()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ConsumerOptions properties = GetConsumerOptions("input", rabbitBindingsOptions);
        RabbitConsumerOptions rabbitConsumerOptions = rabbitBindingsOptions.GetRabbitConsumerOptions("input");
        rabbitConsumerOptions.DeclareExchange = false;
        rabbitConsumerOptions.QueueNameGroupOnly = true;

        IBinding consumerBinding = binder.BindConsumer("amq.topic", null, CreateBindableChannel("input", GetDefaultBindingOptions()), properties);
        ILifecycle endpoint = ExtractEndpoint(consumerBinding);
        var container = GetPropertyValue<DirectMessageListenerContainer>(endpoint, "MessageListenerContainer");

        string queueName = container.GetQueueNames()[0];

        Assert.StartsWith("anonymous.", queueName);
        Assert.True(container.IsRunning);

        consumerBinding.UnbindAsync();
        Assert.False(container.IsRunning);
    }

    [Fact]
    public void TestAnonWithBuiltInExchangeCustomPrefix()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ConsumerOptions properties = GetConsumerOptions("input", rabbitBindingsOptions);
        RabbitConsumerOptions rabbitConsumerOptions = rabbitBindingsOptions.GetRabbitConsumerOptions("input");
        rabbitConsumerOptions.DeclareExchange = false;
        rabbitConsumerOptions.QueueNameGroupOnly = true;
        rabbitConsumerOptions.AnonymousGroupPrefix = "customPrefix.";

        IBinding consumerBinding = binder.BindConsumer("amq.topic", null, CreateBindableChannel("input", GetDefaultBindingOptions()), properties);
        ILifecycle endpoint = ExtractEndpoint(consumerBinding);
        var container = GetPropertyValue<DirectMessageListenerContainer>(endpoint, "MessageListenerContainer");

        string queueName = container.GetQueueNames()[0];
        Assert.StartsWith("customPrefix.", queueName);
        Assert.True(container.IsRunning);

        consumerBinding.UnbindAsync();
        Assert.False(container.IsRunning);
    }

    [Fact]
    public async Task TestConsumerPropertiesWithUserInfrastructureCustomExchangeAndRk()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ConsumerOptions properties = GetConsumerOptions("input", rabbitBindingsOptions);
        RabbitConsumerOptions rabbitConsumerOptions = rabbitBindingsOptions.GetRabbitConsumerOptions("input");

        rabbitConsumerOptions.ExchangeType = ExchangeType.Direct;
        rabbitConsumerOptions.BindingRoutingKey = "foo,bar";
        rabbitConsumerOptions.BindingRoutingKeyDelimiter = ",";
        rabbitConsumerOptions.QueueNameGroupOnly = true;

        // properties.Extension.DelayedExchange = true; // requires delayed message

        // exchange plugin; tested locally
        const string group = "infra";
        IBinding consumerBinding = binder.BindConsumer("propsUser2", group, CreateBindableChannel("input", GetDefaultBindingOptions()), properties);
        ILifecycle endpoint = ExtractEndpoint(consumerBinding);
        var container = GetPropertyValue<DirectMessageListenerContainer>(endpoint, "MessageListenerContainer");

        Assert.True(container.IsRunning);
        await consumerBinding.UnbindAsync();

        Assert.False(container.IsRunning);
        Assert.Equal(group, container.GetQueueNames()[0]);

        var client = new Client();
        IEnumerable<EasyNetQ.Management.Client.Model.Binding> bindings = await client.GetBindingsBySourceAsync("/", "propsUser2");
        int n = 0;

        while (n++ < 100 && (bindings == null || !bindings.Any()))
        {
            Thread.Sleep(100);
            bindings = await client.GetBindingsBySourceAsync("/", "propsUser2");
        }

        Assert.Equal(2, bindings.Count());

        Assert.Equal("propsUser2", bindings.ElementAt(0).Source);
        Assert.Equal(group, bindings.ElementAt(0).Destination);

        Assert.Contains(bindings.ElementAt(0).RoutingKey, new List<string>
        {
            "foo",
            "bar"
        });

        Assert.Equal("propsUser2", bindings.ElementAt(1).Source);
        Assert.Equal(group, bindings.ElementAt(1).Destination);

        Assert.Contains(bindings.ElementAt(1).RoutingKey, new List<string>
        {
            "foo",
            "bar"
        });

        Assert.NotEqual(bindings.ElementAt(1).RoutingKey, bindings.ElementAt(0).RoutingKey);

        Exchange exchange = await client.GetExchangeAsync("/", "propsUser2");

        while (n++ < 100 && exchange == null)
        {
            Thread.Sleep(100);
            exchange = await client.GetExchangeAsync("/", "propsUser2");
        }

        Assert.Equal("direct", exchange.Type);
        Assert.True(exchange.Durable);
        Assert.False(exchange.AutoDelete);
    }

    [Fact]
    public async Task TestConsumerPropertiesWithUserInfrastructureCustomQueueArgs()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ConsumerOptions properties = GetConsumerOptions("input", rabbitBindingsOptions);
        RabbitConsumerOptions extProps = rabbitBindingsOptions.GetRabbitConsumerOptions("input");

        extProps.ExchangeType = ExchangeType.Direct;
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
        extProps.DeadLetterExchangeType = ExchangeType.Topic;
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

        IBinding consumerBinding = binder.BindConsumer("propsUser3", "infra", CreateBindableChannel("input", GetDefaultBindingOptions()), properties);
        ILifecycle endpoint = ExtractEndpoint(consumerBinding);
        var container = GetPropertyValue<DirectMessageListenerContainer>(endpoint, "MessageListenerContainer");

        Assert.True(container.IsRunning);

        var client = new Client();
        IEnumerable<EasyNetQ.Management.Client.Model.Binding> bindings = await client.GetBindingsBySourceAsync("/", "propsUser3");

        int n = 0;

        while (n++ < 100 && (bindings == null || !bindings.Any()))
        {
            Thread.Sleep(100);
            bindings = await client.GetBindingsBySourceAsync("/", "propsUser3");
        }

        Assert.Single(bindings);
        Assert.Equal("propsUser3", bindings.ElementAt(0).Source);
        Assert.Equal("propsUser3.infra", bindings.ElementAt(0).Destination);
        Assert.Equal("foo", bindings.ElementAt(0).RoutingKey);

        bindings = await client.GetBindingsBySourceAsync("/", "customDLX");
        n = 0;

        while (n++ < 100 && (bindings == null || !bindings.Any()))
        {
            Thread.Sleep(100);
            bindings = await client.GetBindingsBySourceAsync("/", "customDLX");
        }

        Assert.Equal("customDLX", bindings.ElementAt(0).Source);
        Assert.Equal("customDLQ", bindings.ElementAt(0).Destination);
        Assert.Equal("customDLRK", bindings.ElementAt(0).RoutingKey);

        Exchange exchange = await client.GetExchangeAsync("/", "propsUser3");
        n = 0;

        while (n++ < 100 && exchange == null)
        {
            Thread.Sleep(100);
            exchange = await client.GetExchangeAsync("/", "propsUser3");
        }

        Assert.Equal("direct", exchange.Type);
        Assert.False(exchange.Durable);
        Assert.True(exchange.AutoDelete);

        exchange = await client.GetExchangeAsync("/", "customDLX");
        n = 0;

        while (n++ < 100 && exchange == null)
        {
            Thread.Sleep(100);
            exchange = await client.GetExchangeAsync("/", "customDLX");
        }

        Assert.Equal("topic", exchange.Type);
        Assert.True(exchange.Durable);
        Assert.False(exchange.AutoDelete);

        EasyNetQ.Management.Client.Model.Queue queue = await client.GetQueueAsync("/", "propsUser3.infra");
        n = 0;

        while (n++ < 100 && (queue == null || queue.Consumers == 0))
        {
            Thread.Sleep(100);
            queue = await client.GetQueueAsync("/", "propsUser3.infra");
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

        queue = await client.GetQueueAsync("/", "customDLQ");

        n = 0;

        while (n++ < 100 && queue == null)
        {
            Thread.Sleep(100);
            queue = await client.GetQueueAsync("/", "customDLQ");
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

        await consumerBinding.UnbindAsync();
        Assert.False(container.IsRunning);
    }

    [Fact]
    public async Task TestConsumerPropertiesWithHeaderExchanges()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ConsumerOptions properties = GetConsumerOptions("input", rabbitBindingsOptions);
        RabbitConsumerOptions rabbitConsumerOptions = rabbitBindingsOptions.GetRabbitConsumerOptions("input");
        rabbitConsumerOptions.ExchangeType = ExchangeType.Headers;
        rabbitConsumerOptions.AutoBindDlq = true;
        rabbitConsumerOptions.DeadLetterExchange = ExchangeType.Headers;
        rabbitConsumerOptions.DeadLetterExchange = "propsHeader.dlx";

        var queueBindingArguments = new Dictionary<string, string>
        {
            { "x-match", "any" },
            { "foo", "bar" }
        };

        rabbitConsumerOptions.QueueBindingArguments = queueBindingArguments;
        rabbitConsumerOptions.DlqBindingArguments = queueBindingArguments;

        const string group = "bindingArgs";
        IBinding consumerBinding = binder.BindConsumer("propsHeader", group, CreateBindableChannel("input", GetDefaultBindingOptions()), properties);
        ILifecycle endpoint = ExtractEndpoint(consumerBinding);
        var container = GetPropertyValue<DirectMessageListenerContainer>(endpoint, "MessageListenerContainer");

        Assert.True(container.IsRunning);
        await consumerBinding.UnbindAsync();

        Assert.False(container.IsRunning);
        Assert.Equal($"propsHeader.{group}", container.GetQueueNames()[0]);

        var client = new Client();
        IEnumerable<EasyNetQ.Management.Client.Model.Binding> bindings = await client.GetBindingsBySourceAsync("/", "propsHeader");

        int n = 0;

        while (n++ < 100 && (bindings == null || !bindings.Any()))
        {
            Thread.Sleep(100);
            bindings = await client.GetBindingsBySourceAsync("/", "propsHeader");
        }

        Assert.Single(bindings);
        EasyNetQ.Management.Client.Model.Binding binding = bindings.First();
        Assert.Equal("propsHeader", binding.Source);
        Assert.Equal($"propsHeader.{group}", binding.Destination);
        Assert.Contains(binding.Arguments, arg => arg.Key == "x-match" && arg.Value == "any");
        Assert.Contains(binding.Arguments, arg => arg.Key == "foo" && arg.Value == "bar");

        bindings = await client.GetBindingsBySourceAsync("/", "propsHeader.dlx");
        n = 0;

        while (n++ < 100 && (bindings == null || !bindings.Any()))
        {
            Thread.Sleep(100);
            bindings = await client.GetBindingsBySourceAsync("/", "propsHeader.dlx");
        }

        Assert.Single(bindings);
        binding = bindings.First();
        Assert.Equal("propsHeader.dlx", binding.Source);
        Assert.Equal($"propsHeader.{group}.dlq", binding.Destination);
        Assert.Contains(binding.Arguments, arg => arg.Key == "x-match" && arg.Value == "any");
        Assert.Contains(binding.Arguments, arg => arg.Key == "foo" && arg.Value == "bar");
    }

    [Fact]
    public void TestProducerProperties()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        BindingOptions bindingOptions = GetDefaultBindingOptions();

        ProducerOptions producerOptions = GetProducerOptions("input", rabbitBindingsOptions);
        IBinding producerBinding = binder.BindProducer("props.0", CreateBindableChannel("input", bindingOptions), producerOptions);

        var endpoint = ExtractEndpoint(producerBinding) as RabbitOutboundEndpoint;
        Assert.Equal(MessageDeliveryMode.Persistent, endpoint.DefaultDeliveryMode);

        var mapper = GetPropertyValue<DefaultRabbitHeaderMapper>(endpoint, "HeaderMapper");
        Assert.NotNull(mapper);
        Assert.NotNull(mapper.RequestHeaderMatcher);

        var matchers =
            GetPropertyValue<List<Integration.Mapping.AbstractHeaderMapper<IMessageHeaders>.IHeaderMatcher>>(mapper.RequestHeaderMatcher, "Matchers");

        Assert.NotNull(matchers);
        Assert.Equal(4, matchers.Count);

        producerBinding.UnbindAsync();
        Assert.False(endpoint.IsRunning);

        Assert.False(endpoint.Template.IsChannelTransacted);

        rabbitBindingsOptions.Bindings.Remove("input");
        ProducerOptions producerProperties = GetProducerOptions("input", rabbitBindingsOptions);
        RabbitProducerOptions producerRabbitOptions = rabbitBindingsOptions.GetRabbitProducerOptions("input");
        binder.ApplicationContext.Register("pkExtractor", new TestPartitionSupport("pkExtractor"));
        binder.ApplicationContext.Register("pkSelector", new TestPartitionSupport("pkSelector"));

        producerProperties.PartitionKeyExtractorName = "pkExtractor";
        producerProperties.PartitionSelectorName = "pkSelector";
        producerRabbitOptions.Prefix = "foo.";
        producerRabbitOptions.DeliveryMode = MessageDeliveryMode.NonPersistent;

        producerRabbitOptions.HeaderPatterns = new[]
        {
            "foo"
        }.ToList();

        producerProperties.PartitionKeyExpression = "'foo'";
        producerProperties.PartitionSelectorExpression = "0";
        producerProperties.PartitionCount = 1;
        producerRabbitOptions.Transacted = true;
        producerRabbitOptions.DelayExpression = "42";

        producerProperties.RequiredGroups = new[]
        {
            "prodPropsRequired"
        }.ToList();

        BindingOptions producerBindingProperties = CreateProducerBindingOptions(producerProperties);
        DirectChannel channel = CreateBindableChannel("output", producerBindingProperties);

        producerBinding = binder.BindProducer("props.0", channel, producerProperties);

        endpoint = ExtractEndpoint(producerBinding) as RabbitOutboundEndpoint;
        Assert.Same(GetResource(), endpoint.Template.ConnectionFactory);

        Assert.Equal($"'props.0-' + Headers['{BinderHeaders.PartitionHeader}']", endpoint.RoutingKeyExpression.ExpressionString);
        Assert.Equal("42", endpoint.DelayExpression.ExpressionString);
        Assert.Equal(MessageDeliveryMode.NonPersistent, endpoint.DefaultDeliveryMode);
        Assert.True(endpoint.Template.IsChannelTransacted);

        mapper = GetPropertyValue<DefaultRabbitHeaderMapper>(endpoint, "HeaderMapper");
        Assert.NotNull(mapper);
        Assert.NotNull(mapper.RequestHeaderMatcher);
        matchers = GetPropertyValue<List<Integration.Mapping.AbstractHeaderMapper<IMessageHeaders>.IHeaderMatcher>>(mapper.RequestHeaderMatcher, "Matchers");
        Assert.NotNull(matchers);
        Assert.Equal(4, matchers.Count);
        Assert.Equal("foo", GetPropertyValue<string>(matchers[3], "Pattern"));

        IMessage message = MessageBuilder.WithPayload("foo").Build();
        channel.Send(message);
        IMessage received = new RabbitTemplate(GetResource()).Receive("foo.props.0.prodPropsRequired-0", 10_000);
        Assert.NotNull(received);

        Assert.Equal(42, received.Headers[RabbitMessageHeaders.ReceivedDelay]);
        producerBinding.UnbindAsync();
        Assert.False(endpoint.IsRunning);
    }

    [Fact]
    public void TestDurablePubSubWithAutoBindDlq()
    {
        ILogger<RabbitAdmin> logger = LoggerFactory.CreateLogger<RabbitAdmin>();
        var admin = new RabbitAdmin(GetResource(), logger);
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ConsumerOptions consumerProperties = GetConsumerOptions("input", rabbitBindingsOptions);
        RabbitConsumerOptions rabbitConsumerOptions = rabbitBindingsOptions.GetRabbitConsumerOptions("input");

        rabbitConsumerOptions.Prefix = TestPrefix;
        rabbitConsumerOptions.AutoBindDlq = true;
        rabbitConsumerOptions.DurableSubscription = true;
        consumerProperties.MaxAttempts = 1; // disable retry
        DirectChannel moduleInputChannel = CreateBindableChannel("input", CreateConsumerBindingOptions(consumerProperties));
        moduleInputChannel.ComponentName = "durableTest";

        moduleInputChannel.Subscribe(new TestMessageHandler
        {
            OnHandleMessage = _ => throw new Exception("foo")
        });

        IBinding consumerBinding = binder.BindConsumer("durabletest.0", "tgroup", moduleInputChannel, consumerProperties);

        var template = new RabbitTemplate(GetResource());
        template.ConvertAndSend($"{TestPrefix}durabletest.0", string.Empty, "foo");

        int n = 0;

        while (n++ < 100)
        {
            string deadLetter = template.ReceiveAndConvert<string>($"{TestPrefix}durabletest.0.tgroup.dlq");

            if (deadLetter != null)
            {
                Assert.Equal("foo", deadLetter);
                break;
            }

            Thread.Sleep(100);
        }

        Assert.InRange(n, 0, 150);

        consumerBinding.UnbindAsync();
        Assert.NotNull(admin.GetQueueProperties($"{TestPrefix}durabletest.0.tgroup.dlq"));
    }

    [Fact]
    public void TestNonDurablePubSubWithAutoBindDlq()
    {
        ILogger<RabbitAdmin> logger = LoggerFactory.CreateLogger<RabbitAdmin>();
        var admin = new RabbitAdmin(GetResource(), logger);

        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ConsumerOptions consumerProperties = GetConsumerOptions("input", rabbitBindingsOptions);
        RabbitConsumerOptions rabbitConsumerOptions = rabbitBindingsOptions.GetRabbitConsumerOptions("input");

        rabbitConsumerOptions.Prefix = TestPrefix;
        rabbitConsumerOptions.AutoBindDlq = true;
        rabbitConsumerOptions.DurableSubscription = false;
        consumerProperties.MaxAttempts = 1; // disable retry
        BindingOptions bindingProperties = CreateConsumerBindingOptions(consumerProperties);
        DirectChannel moduleInputChannel = CreateBindableChannel("input", bindingProperties);
        moduleInputChannel.ComponentName = "nondurabletest";

        moduleInputChannel.Subscribe(new TestMessageHandler
        {
            OnHandleMessage = _ => throw new Exception("foo")
        });

        IBinding consumerBinding = binder.BindConsumer("nondurabletest.0", "tgroup", moduleInputChannel, consumerProperties);

        consumerBinding.UnbindAsync();
        Assert.Null(admin.GetQueueProperties($"{TestPrefix}nondurabletest.0.dlq"));
    }

    [Fact]
    public void TestAutoBindDlq()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ConsumerOptions consumerProperties = GetConsumerOptions("input", rabbitBindingsOptions);
        RabbitConsumerOptions rabbitConsumerOptions = rabbitBindingsOptions.GetRabbitConsumerOptions("input");

        rabbitConsumerOptions.Prefix = TestPrefix;
        rabbitConsumerOptions.AutoBindDlq = true;
        consumerProperties.MaxAttempts = 1; // disable retry
        rabbitConsumerOptions.DurableSubscription = true;
        BindingOptions bindingProperties = CreateConsumerBindingOptions(consumerProperties);
        DirectChannel moduleInputChannel = CreateBindableChannel("input", bindingProperties);
        moduleInputChannel.ComponentName = "dlqTest";

        moduleInputChannel.Subscribe(new TestMessageHandler
        {
            OnHandleMessage = _ => throw new Exception("foo")
        });

        consumerProperties.Multiplex = true;
        IBinding consumerBinding = binder.BindConsumer("dlqtest,dlqtest2", "default", moduleInputChannel, consumerProperties);

        ILifecycle endpoint = ExtractEndpoint(consumerBinding);
        var container = GetPropertyValue<AbstractMessageListenerContainer>(endpoint, "MessageListenerContainer");
        Assert.Equal(2, container.GetQueueNames().Length);

        var template = new RabbitTemplate(GetResource());
        template.ConvertAndSend(string.Empty, $"{TestPrefix}dlqtest.default", "foo");

        int n = 0;

        while (n++ < 100)
        {
            string deadLetter = template.ReceiveAndConvert<string>($"{TestPrefix}dlqtest.default.dlq");

            if (deadLetter != null)
            {
                Assert.Equal("foo", deadLetter);
                break;
            }

            Thread.Sleep(100);
        }

        Assert.InRange(n, 0, 99);

        template.ConvertAndSend(string.Empty, $"{TestPrefix}dlqtest2.default", "bar");

        n = 0;

        while (n++ < 100)
        {
            string deadLetter = template.ReceiveAndConvert<string>($"{TestPrefix}dlqtest2.default.dlq");

            if (deadLetter != null)
            {
                Assert.Equal("bar", deadLetter);
                break;
            }

            Thread.Sleep(100);
        }

        Assert.InRange(n, 0, 99);
        consumerBinding.UnbindAsync();

        var provider = GetPropertyValue<RabbitExchangeQueueProvisioner>(binder.Binder, "ProvisioningProvider");
        var context = GetFieldValue<GenericApplicationContext>(provider, "_autoDeclareContext");

        Assert.False(context.ContainsService($"{TestPrefix}dlqtest.default.binding"));
        Assert.False(context.ContainsService($"{TestPrefix}dlqtest.default"));
        Assert.False(context.ContainsService($"{TestPrefix}dlqtest.default.dlq.binding"));
        Assert.False(context.ContainsService($"{TestPrefix}dlqtest.default.dlq"));
    }

    [Fact]
    public async Task TestAutoBindDlqManualAcks()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ConsumerOptions consumerProperties = GetConsumerOptions("input", rabbitBindingsOptions);
        RabbitConsumerOptions rabbitConsumerOptions = rabbitBindingsOptions.GetRabbitConsumerOptions("input");
        rabbitConsumerOptions.Prefix = TestPrefix;
        rabbitConsumerOptions.AutoBindDlq = true;
        consumerProperties.MaxAttempts = 2;
        rabbitConsumerOptions.DurableSubscription = true;
        rabbitConsumerOptions.AcknowledgeMode = AcknowledgeMode.Manual;
        BindingOptions bindingProperties = CreateConsumerBindingOptions(consumerProperties);

        DirectChannel moduleInputChannel = CreateBindableChannel("input", bindingProperties);
        moduleInputChannel.ComponentName = "dlqTestManual";

        var client = new Client();
        Vhost vhost = client.GetVhost("/");

        moduleInputChannel.Subscribe(new TestMessageHandler
        {
            // Wait until unacked state is reflected in the admin
            OnHandleMessage = _ =>
            {
                EasyNetQ.Management.Client.Model.Queue info = client.GetQueue($"{TestPrefix}dlqTestManual.default", vhost);
                int n = 0;

                while (n++ < 100 && info.MessagesUnacknowledged < 1L)
                {
                    Thread.Sleep(100);
                    info = client.GetQueue($"{TestPrefix}dlqTestManual.default", vhost);
                }

                throw new Exception("foo");
            }
        });

        IBinding consumerBinding = binder.BindConsumer("dlqTestManual", "default", moduleInputChannel, consumerProperties);

        var template = new RabbitTemplate(GetResource());
        template.ConvertAndSend(string.Empty, $"{TestPrefix}dlqTestManual.default", "foo");

        int n = 0;

        while (n++ < 100)
        {
            string deadLetter = template.ReceiveAndConvert<string>($"{TestPrefix}dlqTestManual.default.dlq");

            if (deadLetter != null)
            {
                Assert.Equal("foo", deadLetter);
                break;
            }

            Thread.Sleep(200);
        }

        Assert.InRange(n, 1, 100);

        n = 0;
        EasyNetQ.Management.Client.Model.Queue info = client.GetQueue($"{TestPrefix}dlqTestManual.default", vhost);

        while (n++ < 100 && info.MessagesUnacknowledged > 0L)
        {
            Thread.Sleep(200);
            info = client.GetQueue($"{TestPrefix}dlqTestManual.default", vhost);
        }

        Assert.Equal(0, info.MessagesUnacknowledged);

        await consumerBinding.UnbindAsync();

        var provider = GetPropertyValue<RabbitExchangeQueueProvisioner>(binder.Binder, "ProvisioningProvider");
        var context = GetFieldValue<GenericApplicationContext>(provider, "_autoDeclareContext");

        Assert.False(context.ContainsService($"{TestPrefix}dlqTestManual.default.binding"));
        Assert.False(context.ContainsService($"{TestPrefix}dlqTestManual.default"));
        Assert.False(context.ContainsService($"{TestPrefix}dlqTestManual.default.dlq.binding"));
        Assert.False(context.ContainsService($"{TestPrefix}dlqTestManual.default.dlq"));
    }

    [Fact]
    public void TestOptions()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();

        RabbitProducerOptions producerOptions = rabbitBindingsOptions.GetRabbitProducerOptions("test");
        producerOptions.Prefix = "rets";
        RabbitProducerOptions producerOptions2 = rabbitBindingsOptions.GetRabbitProducerOptions("test");

        Assert.Equal(producerOptions.Prefix, producerOptions2.Prefix);
    }

    [Fact]
    public void TestAutoBindDlqPartionedConsumerFirst()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ConsumerOptions consumerProperties = GetConsumerOptions("input", rabbitBindingsOptions);
        RabbitConsumerOptions rabbitConsumerOptions = rabbitBindingsOptions.GetRabbitConsumerOptions("input");
        rabbitConsumerOptions.Prefix = "bindertest.";
        rabbitConsumerOptions.AutoBindDlq = true;
        consumerProperties.MaxAttempts = 1; // disable retry
        consumerProperties.Partitioned = true;
        consumerProperties.InstanceIndex = 0;

        DirectChannel input0 = CreateBindableChannel("input", CreateConsumerBindingOptions(consumerProperties));
        input0.ComponentName = "test.input0DLQ";
        IBinding input0Binding = binder.BindConsumer("partDLQ.0", "dlqPartGrp", input0, consumerProperties);
        IBinding defaultConsumerBinding1 = binder.BindConsumer("partDLQ.0", "default", new QueueChannel(), consumerProperties);
        consumerProperties.InstanceIndex = 1;

        DirectChannel input1 = CreateBindableChannel("input1", CreateConsumerBindingOptions(consumerProperties));
        input1.ComponentName = "test.input1DLQ";
        IBinding input1Binding = binder.BindConsumer("partDLQ.0", "dlqPartGrp", input1, consumerProperties);

        IBinding defaultConsumerBinding2 = binder.BindConsumer("partDLQ.0", "default", new QueueChannel(), consumerProperties);

        ProducerOptions producerProperties = GetProducerOptions("output", rabbitBindingsOptions);
        RabbitProducerOptions rabbitProducerOptions = rabbitBindingsOptions.GetRabbitProducerOptions("output");
        rabbitProducerOptions.Prefix = "bindertest.";

        binder.ApplicationContext.Register("pkExtractor", new TestPartitionSupport("pkExtractor"));
        binder.ApplicationContext.Register("pkSelector", new TestPartitionSupport("pkSelector"));

        rabbitProducerOptions.AutoBindDlq = true;
        producerProperties.PartitionKeyExtractorName = "pkExtractor";
        producerProperties.PartitionSelectorName = "pkSelector";
        producerProperties.PartitionCount = 2;

        BindingOptions bindingProperties = CreateProducerBindingOptions(producerProperties);

        DirectChannel output = CreateBindableChannel("output", bindingProperties);
        output.ComponentName = "test.output";
        IBinding outputBinding = binder.BindProducer("partDLQ.0", output, producerProperties);

        var latch0 = new CountdownEvent(1);

        input0.Subscribe(new TestMessageHandler
        {
            OnHandleMessage = _ =>
            {
                if (latch0.CurrentCount <= 0)
                {
                    throw new Exception("dlq");
                }

                latch0.Signal();
            }
        });

        var latch1 = new CountdownEvent(1);

        input1.Subscribe(new TestMessageHandler
        {
            OnHandleMessage = _ =>
            {
                if (latch1.CurrentCount <= 0)
                {
                    throw new Exception("dlq");
                }

                latch1.Signal();
            }
        });

        IMessage message = MessageBuilder.WithPayload(1).Build();
        output.Send(message);
        Assert.True(latch1.Wait(TimeSpan.FromSeconds(10)));

        output.Send(Message.Create(0));
        Assert.True(latch0.Wait(TimeSpan.FromSeconds(10)));

        output.Send(Message.Create(1));

        var template = new RabbitTemplate(GetResource());
        template.ReceiveTimeout = 10000;

        const string streamDlqName = "bindertest.partDLQ.0.dlqPartGrp.dlq";

        IMessage received = template.Receive(streamDlqName);
        Assert.NotNull(received);

        Assert.Equal("bindertest.partDLQ.0.dlqPartGrp-1", received.Headers.ReceivedRoutingKey());
        Assert.DoesNotContain(BinderHeaders.PartitionHeader, received.Headers.Select(h => h.Key));

        output.Send(Message.Create(0));
        received = template.Receive(streamDlqName);
        Assert.NotNull(received);
        Assert.Equal("bindertest.partDLQ.0.dlqPartGrp-0", received.Headers.ReceivedRoutingKey());
        Assert.DoesNotContain(BinderHeaders.PartitionHeader, received.Headers.Select(h => h.Key));

        input0Binding.UnbindAsync();
        input1Binding.UnbindAsync();
        defaultConsumerBinding1.UnbindAsync();
        defaultConsumerBinding2.UnbindAsync();
        outputBinding.UnbindAsync();
    }

    [Fact]
    public void TestAutoBindDlqPartitionedConsumerFirstWithRepublishNoRetry()
    {
        TestAutoBindDlqPartionedConsumerFirstWithRepublishGuts(false);
    }

    [Fact]
    public void TestAutoBindDlqPartitionedConsumerFirstWithRepublishWithRetry()
    {
        TestAutoBindDlqPartionedConsumerFirstWithRepublishGuts(true);
    }

    [Fact]
    public void TestAutoBindDlqPartitionedProducerFirst()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ConsumerOptions consumerProperties = GetConsumerOptions("input", rabbitBindingsOptions);
        RabbitConsumerOptions rabbitConsumerOptions = rabbitBindingsOptions.GetRabbitConsumerOptions("input");
        ProducerOptions properties = GetProducerOptions("output", rabbitBindingsOptions);
        RabbitProducerOptions rabbitProducerOptions = rabbitBindingsOptions.GetRabbitProducerOptions("output");
        rabbitProducerOptions.Prefix = "bindertest.";
        rabbitProducerOptions.AutoBindDlq = true;

        properties.RequiredGroups = new[]
        {
            "dlqPartGrp"
        }.ToList();

        binder.ApplicationContext.Register("pkExtractor", new TestPartitionSupport("pkExtractor"));
        properties.PartitionKeyExtractorName = "pkExtractor";
        properties.PartitionSelectorName = "pkExtractor";
        properties.PartitionCount = 2;
        DirectChannel output = CreateBindableChannel("output", CreateProducerBindingOptions(properties));
        output.ComponentName = "test.output";
        IBinding outputBinding = binder.BindProducer("partDLQ.1", output, properties);

        rabbitConsumerOptions.Prefix = "bindertest.";
        rabbitConsumerOptions.AutoBindDlq = true;
        consumerProperties.MaxAttempts = 1; // disable retry
        consumerProperties.Partitioned = true;
        consumerProperties.InstanceIndex = 0;
        DirectChannel input0 = CreateBindableChannel("input", CreateConsumerBindingOptions(consumerProperties));
        input0.ComponentName = "test.input0DLQ";
        IBinding input0Binding = binder.BindConsumer("partDLQ.1", "dlqPartGrp", input0, consumerProperties);
        IBinding defaultConsumerBinding1 = binder.BindConsumer("partDLQ.1", "defaultConsumer", new QueueChannel(), consumerProperties);
        consumerProperties.InstanceIndex = 1;
        DirectChannel input1 = CreateBindableChannel("input1", CreateConsumerBindingOptions(consumerProperties));
        input1.ComponentName = "test.input1DLQ";
        IBinding input1Binding = binder.BindConsumer("partDLQ.1", "dlqPartGrp", input1, consumerProperties);
        IBinding defaultConsumerBinding2 = binder.BindConsumer("partDLQ.1", "defaultConsumer", new QueueChannel(), consumerProperties);

        var latch0 = new CountdownEvent(1);

        input0.Subscribe(new TestMessageHandler
        {
            OnHandleMessage = _ =>
            {
                if (latch0.CurrentCount <= 0)
                {
                    throw new Exception("dlq");
                }

                latch0.Signal();
            }
        });

        var latch1 = new CountdownEvent(1);

        input1.Subscribe(new TestMessageHandler
        {
            OnHandleMessage = _ =>
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

        var template = new RabbitTemplate(GetResource());
        template.ReceiveTimeout = 10000;

        const string streamDlqName = "bindertest.partDLQ.1.dlqPartGrp.dlq";

        IMessage received = template.Receive(streamDlqName);
        Assert.NotNull(received);
        Assert.Equal("bindertest.partDLQ.1.dlqPartGrp-1", received.Headers.ReceivedRoutingKey());
        Assert.Equal(MessageDeliveryMode.Persistent, received.Headers.ReceivedDeliveryMode());
        Assert.DoesNotContain(BinderHeaders.PartitionHeader, received.Headers);

        output.Send(Message.Create(0));
        received = template.Receive(streamDlqName);
        Assert.NotNull(received);

        Assert.Equal("bindertest.partDLQ.1.dlqPartGrp-0", received.Headers.ReceivedRoutingKey());

        Assert.DoesNotContain(BinderHeaders.PartitionHeader, received.Headers);

        input0Binding.UnbindAsync();
        input1Binding.UnbindAsync();
        defaultConsumerBinding1.UnbindAsync();
        defaultConsumerBinding2.UnbindAsync();
        outputBinding.UnbindAsync();
    }

    [Fact]
    public void TestAutoBindDlQWithRepublish()
    {
        maxStackTraceSize = RabbitUtils.GetMaxFrame(GetResource()) - 20_000;
        Assert.True(maxStackTraceSize > 0);

        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ConsumerOptions consumerProperties = GetConsumerOptions("input", rabbitBindingsOptions);
        RabbitConsumerOptions rabbitConsumerOptions = rabbitBindingsOptions.GetRabbitConsumerOptions("input");

        rabbitConsumerOptions.Prefix = TestPrefix;
        rabbitConsumerOptions.AutoBindDlq = true;
        rabbitConsumerOptions.RepublishToDlq = true;
        consumerProperties.MaxAttempts = 1; // disable retry
        rabbitConsumerOptions.DurableSubscription = true;
        DirectChannel moduleInputChannel = CreateBindableChannel("input", CreateConsumerBindingOptions(consumerProperties));
        moduleInputChannel.ComponentName = "dlqPubTest";
        Exception exception = BigCause();

        Assert.True(exception.StackTrace.Length > maxStackTraceSize);
        var noNotRepublish = new AtomicBoolean();

        moduleInputChannel.Subscribe(new TestMessageHandler
        {
            OnHandleMessage = _ =>
            {
                if (noNotRepublish.Value)
                {
                    throw new ImmediateAcknowledgeException("testDoNotRepublish");
                }

                ExceptionDispatchInfo.Capture(exception).Throw();
            }
        });

        consumerProperties.Multiplex = true;
        IBinding consumerBinding = binder.BindConsumer("foo.dlqpubtest,foo.dlqpubtest2", "foo", moduleInputChannel, consumerProperties);

        var template = new RabbitTemplate(GetResource());
        template.ConvertAndSend(string.Empty, $"{TestPrefix}foo.dlqpubtest.foo", "foo");

        template.ReceiveTimeout = 10_000;

        IMessage deadLetter = template.Receive($"{TestPrefix}foo.dlqpubtest.foo.dlq");
        Assert.NotNull(deadLetter);
        Assert.Equal("foo", ((byte[])deadLetter.Payload).GetString());
        Assert.Contains(RepublishMessageRecoverer.XExceptionStacktrace, deadLetter.Headers);

        // Assert.Equal(maxStackTraceSize, ((string)deadLetter.Headers[RepublishMessageRecoverer.X_EXCEPTION_STACKTRACE]).Length); TODO: Wrapped exception doesn't contain propagated stack trace
        template.ConvertAndSend(string.Empty, $"{TestPrefix}foo.dlqpubtest2.foo", "bar");

        deadLetter = template.Receive($"{TestPrefix}foo.dlqpubtest2.foo.dlq");
        Assert.NotNull(deadLetter);

        Assert.Equal("bar", ((byte[])deadLetter.Payload).GetString());
        Assert.Contains(RepublishMessageRecoverer.XExceptionStacktrace, deadLetter.Headers);

        noNotRepublish.GetAndSet(true);
        template.ConvertAndSend(string.Empty, $"{TestPrefix}foo.dlqpubtest2.foo", "baz");
        template.ReceiveTimeout = 500;
        Assert.Null(template.Receive($"{TestPrefix}foo.dlqpubtest2.foo.dlq"));

        consumerBinding.UnbindAsync();
    }

    [Fact]
    public void TestBatchingAndCompression()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ProducerOptions producerProperties = GetProducerOptions("output", rabbitBindingsOptions);
        RabbitProducerOptions rabbitProducerOptions = rabbitBindingsOptions.GetRabbitProducerOptions("output");

        rabbitProducerOptions.DeliveryMode = MessageDeliveryMode.NonPersistent;
        rabbitProducerOptions.BatchingEnabled = true;
        rabbitProducerOptions.BatchSize = 2;
        rabbitProducerOptions.BatchBufferLimit = 100000;
        rabbitProducerOptions.BatchTimeout = 30000;
        rabbitProducerOptions.Compress = true;

        producerProperties.RequiredGroups = new[]
        {
            "default"
        }.ToList();

        DirectChannel output = CreateBindableChannel("output", CreateProducerBindingOptions(producerProperties));
        output.ComponentName = "batchingProducer";
        IBinding producerBinding = binder.BindProducer("batching.0", output, producerProperties);

        var postProcessor = binder.Binder.CompressingPostProcessor as GZipPostProcessor;
        Assert.Equal(CompressionLevel.Fastest, postProcessor.Level);

        IMessage<byte[]> fooMessage = Message.Create("foo".GetBytes());
        IMessage<byte[]> barMessage = Message.Create("bar".GetBytes());

        output.Send(fooMessage);
        output.Send(barMessage);

        object obj = SpyOn("batching.0.default").Receive(false);
        Assert.IsType<byte[]>(obj);
        Assert.Equal("\u0000\u0000\u0000\u0003foo\u0000\u0000\u0000\u0003bar", ((byte[])obj).GetString());

        // TODO: Inject and check log output ...
        //    ArgumentCaptor<Object> captor = ArgumentCaptor.forClass(Object.class);
        // verify(logger).trace(captor.capture());
        // assertThat(captor.getValue().toString()).contains(("Compressed 14 to "));
        var input = new QueueChannel
        {
            ComponentName = "batchingConsumer"
        };

        ConsumerOptions consumerProperties = GetConsumerOptions("input", rabbitBindingsOptions);
        IBinding consumerBinding = binder.BindConsumer("batching.0", "test", input, consumerProperties);

        output.Send(fooMessage);
        output.Send(barMessage);

        var inMessage = (Message<byte[]>)input.Receive(10000);
        Assert.NotNull(inMessage);
        Assert.Equal("foo", inMessage.Payload.GetString());
        inMessage = (Message<byte[]>)input.Receive(10000);

        Assert.NotNull(inMessage);
        Assert.Equal("bar", inMessage.Payload.GetString());
        Assert.Null(inMessage.Headers[RabbitMessageHeaders.DeliveryMode]);

        producerBinding.UnbindAsync();
        consumerBinding.UnbindAsync();
    }

    // TestProducerBatching, TestConsumerBatching only works with SMLC - not implemented in steeltoe
    [Fact]
    public void TestInternalHeadersNotPropagated()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ProducerOptions producerProperties = GetProducerOptions("output", rabbitBindingsOptions);
        RabbitProducerOptions rabbitProducerOptions = rabbitBindingsOptions.GetRabbitProducerOptions("output");

        rabbitProducerOptions.DeliveryMode = MessageDeliveryMode.NonPersistent;

        DirectChannel output = CreateBindableChannel("output", CreateProducerBindingOptions(producerProperties));
        output.ComponentName = "propagate.out";
        IBinding producerBinding = binder.BindProducer("propagate.1", output, producerProperties);

        var input = new QueueChannel
        {
            ComponentName = "propagate.in"
        };

        ConsumerOptions consumerProperties = GetConsumerOptions("input", rabbitBindingsOptions);
        IBinding consumerBinding = binder.BindConsumer("propagate.0", "propagate", input, consumerProperties);

        ILogger<RabbitAdmin> logger = LoggerFactory.CreateLogger<RabbitAdmin>();
        var admin = new RabbitAdmin(GetResource(), logger);

        admin.DeclareQueue(new Queue("propagate"));
        admin.DeclareBinding(new RabbitBinding("propagate_binding", "propagate", RabbitBinding.DestinationType.Queue, "propagate.1", "#", null));
        var template = new RabbitTemplate(GetResource());

        template.ConvertAndSend("propagate.0.propagate", "foo");
        IMessage message = input.Receive(10_000);
        Assert.NotNull(message);
        output.Send(message);
        IMessage received = template.Receive("propagate", 10_000);
        Assert.NotNull(received);

        Assert.Equal("foo".GetBytes(), received.Payload);
        Assert.Null(received.Headers[IntegrationMessageHeaderAccessor.SourceData]);
        Assert.Null(received.Headers[IntegrationMessageHeaderAccessor.DeliveryAttempt]);

        producerBinding.UnbindAsync();
        consumerBinding.UnbindAsync();
        admin.DeleteQueue("propagate");
    }

    /*
     * Test late binding due to broker down; queues with and without DLQs, and partitioned
     * queues.
     */
    [Fact]
    [Trait("Category", "SkipOnLinux")]
    public void TestLateBinding()
    {
        RabbitProxy proxy = null;
        CachingConnectionFactory cf = null;

        try
        {
            proxy = new RabbitProxy(LoggerFactory.CreateLogger<RabbitProxy>());
            cf = new CachingConnectionFactory("127.0.0.1", proxy.Port, LoggerFactory);
            IApplicationContext context = RabbitTestBinder.GetApplicationContext();

            var rabbitBindingsOptions = new TestOptionsMonitor<RabbitBindingsOptions>(new RabbitBindingsOptions());
            RabbitBindingsOptions currentRabbitBindings = rabbitBindingsOptions.CurrentValue;
            var rabbitOptions = new TestOptionsMonitor<RabbitOptions>(new RabbitOptions());

            var provisioner =
                new RabbitExchangeQueueProvisioner(cf, rabbitBindingsOptions, context, LoggerFactory.CreateLogger<RabbitExchangeQueueProvisioner>());

            var binderOptions = new TestOptionsMonitor<RabbitBinderOptions>(new RabbitBinderOptions());

            var rabbitBinder = new RabbitMessageChannelBinder(context, LoggerFactory.CreateLogger<RabbitMessageChannelBinder>(), cf, rabbitOptions,
                binderOptions, rabbitBindingsOptions, provisioner);

            var binder = new RabbitTestBinder(cf, rabbitBinder, LoggerFactory.CreateLogger<RabbitTestBinder>());
            testBinder = binder;

            ProducerOptions producerProperties = GetProducerOptions("output", currentRabbitBindings);
            RabbitProducerOptions rabbitProducerOptions = currentRabbitBindings.GetRabbitProducerOptions("output");
            rabbitProducerOptions.Prefix = "latebinder.";
            rabbitProducerOptions.AutoBindDlq = true;
            rabbitProducerOptions.Transacted = true;

            DirectChannel moduleOutputChannel = CreateBindableChannel("output", CreateProducerBindingOptions(producerProperties));
            IBinding late0ProducerBinding = binder.BindProducer("late.0", moduleOutputChannel, producerProperties);

            var moduleInputChannel = new QueueChannel();
            ConsumerOptions consumerOptions = GetConsumerOptions("input", currentRabbitBindings);
            RabbitConsumerOptions rabbitConsumerOptions = currentRabbitBindings.GetRabbitConsumerOptions("input");
            rabbitConsumerOptions.Prefix = "latebinder.";
            IBinding late0ConsumerBinding = binder.BindConsumer("late.0", "test", moduleInputChannel, consumerOptions);
            producerProperties.PartitionKeyExpression = "Payload.Equals('0') ? 0 : 1";
            producerProperties.PartitionSelectorExpression = "GetHashCode()";
            producerProperties.PartitionCount = 2;

            DirectChannel partOutputChannel = CreateBindableChannel("output", CreateProducerBindingOptions(producerProperties));
            IBinding partlate0ProducerBinding = binder.BindProducer("partlate.0", partOutputChannel, producerProperties);

            var partInputChannel0 = new QueueChannel();
            var partInputChannel1 = new QueueChannel();

            ConsumerOptions partLateConsumerProperties = GetConsumerOptions("partLate", currentRabbitBindings);
            RabbitConsumerOptions partLateRabbitConsumerOptions = currentRabbitBindings.GetRabbitConsumerOptions("partLate");
            partLateRabbitConsumerOptions.Prefix = "latebinder.";
            partLateConsumerProperties.Partitioned = true;
            partLateConsumerProperties.InstanceIndex = 0;

            IBinding partlate0Consumer0Binding = binder.BindConsumer("partlate.0", "test", partInputChannel0, partLateConsumerProperties);
            partLateConsumerProperties.InstanceIndex = 1;
            IBinding partlate0Consumer1Binding = binder.BindConsumer("partlate.0", "test", partInputChannel1, partLateConsumerProperties);

            ProducerOptions noDlqProducerProperties = GetProducerOptions("noDlq", currentRabbitBindings);
            RabbitProducerOptions noDlqRabbitProducerOptions = currentRabbitBindings.GetRabbitProducerOptions("noDlq");
            noDlqRabbitProducerOptions.Prefix = "latebinder.";
            DirectChannel noDlqOutputChannel = CreateBindableChannel("output", CreateProducerBindingOptions(noDlqProducerProperties));
            IBinding noDlqProducerBinding = binder.BindProducer("lateNoDLQ.0", noDlqOutputChannel, noDlqProducerProperties);

            var noDlqInputChannel = new QueueChannel();
            ConsumerOptions noDlqConsumerProperties = GetConsumerOptions("noDlqConsumer", currentRabbitBindings);
            RabbitConsumerOptions noDlqRabbitConsumerOptions = currentRabbitBindings.GetRabbitConsumerOptions("noDlqConsumer");
            noDlqRabbitConsumerOptions.Prefix = "latebinder.";
            IBinding noDlqConsumerBinding = binder.BindConsumer("lateNoDLQ.0", "test", noDlqInputChannel, noDlqConsumerProperties);

            DirectChannel outputChannel = CreateBindableChannel("output", CreateProducerBindingOptions(noDlqProducerProperties));
            IBinding pubSubProducerBinding = binder.BindProducer("latePubSub", outputChannel, noDlqProducerProperties);
            var pubSubInputChannel = new QueueChannel();
            noDlqRabbitConsumerOptions.DurableSubscription = false;
            IBinding nonDurableConsumerBinding = binder.BindConsumer("latePubSub", "lategroup", pubSubInputChannel, noDlqConsumerProperties);

            var durablePubSubInputChannel = new QueueChannel();
            noDlqRabbitConsumerOptions.DurableSubscription = true;
            IBinding durableConsumerBinding = binder.BindConsumer("latePubSub", "lateDurableGroup", durablePubSubInputChannel, noDlqConsumerProperties);

            proxy.Start();

            Thread.Sleep(5000);

            moduleOutputChannel.Send(MessageBuilder.WithPayload("foo").SetHeader(MessageHeaders.ContentType, MimeTypeUtils.TextPlain).Build());

            IMessage message = moduleInputChannel.Receive(20000);
            Assert.NotNull(message);
            Assert.NotNull(message.Payload);

            noDlqOutputChannel.Send(MessageBuilder.WithPayload("bar").SetHeader(MessageHeaders.ContentType, MimeTypeUtils.TextPlain).Build());

            message = noDlqInputChannel.Receive(10000);
            Assert.NotNull(message);
            Assert.Equal("bar".GetBytes(), message.Payload);

            outputChannel.Send(MessageBuilder.WithPayload("baz").SetHeader(MessageHeaders.ContentType, MimeTypeUtils.TextPlain).Build());
            message = pubSubInputChannel.Receive(10000);
            Assert.NotNull(message);
            Assert.Equal("baz".GetBytes(), message.Payload);
            message = durablePubSubInputChannel.Receive(10000);
            Assert.NotNull(message);
            Assert.Equal("baz".GetBytes(), message.Payload);

            partOutputChannel.Send(MessageBuilder.WithPayload("0").SetHeader(MessageHeaders.ContentType, MimeTypeUtils.TextPlain).Build());
            partOutputChannel.Send(MessageBuilder.WithPayload("1").SetHeader(MessageHeaders.ContentType, MimeTypeUtils.TextPlain).Build());

            message = partInputChannel0.Receive(10000);
            Assert.NotNull(message);

            Assert.Equal("0".GetBytes(), message.Payload);
            message = partInputChannel1.Receive(10000);
            Assert.NotNull(message);
            Assert.Equal("1".GetBytes(), message.Payload);

            late0ProducerBinding.UnbindAsync();
            late0ConsumerBinding.UnbindAsync();
            partlate0ProducerBinding.UnbindAsync();
            partlate0Consumer0Binding.UnbindAsync();
            partlate0Consumer1Binding.UnbindAsync();
            noDlqProducerBinding.UnbindAsync();
            noDlqConsumerBinding.UnbindAsync();
            pubSubProducerBinding.UnbindAsync();
            nonDurableConsumerBinding.UnbindAsync();
            durableConsumerBinding.UnbindAsync();

            // Reset timeouts so cleanup happens
            testBinder.ResetConnectionFactoryTimeout();

            Cleanup();
        }
        finally
        {
            proxy?.Stop();
            cf?.Dispose();

            GetResource().Dispose();
        }
    }

    [Fact]
    public async Task TestBadUserDeclarationsFatal()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        IApplicationContext context = binder.ApplicationContext;
        context.Register("testBadUserDeclarationsFatal", new Queue("testBadUserDeclarationsFatal", false));
        context.Register("binder", binder);

        RabbitMessageChannelBinder channelBinder = binder.Binder;
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
                ConsumerOptions consumerOptions = GetConsumerOptions("input", rabbitBindingsOptions);
                binding = binder.BindConsumer("input", "baddecls", CreateBindableChannel("input", GetDefaultBindingOptions()), consumerOptions);
                throw new Exception("Expected exception");
            });
        }
        finally
        {
            admin.DeleteQueue("testBadUserDeclarationsFatal");

            if (binding != null)
            {
                await binding.UnbindAsync();
            }
        }
    }

    [Fact]
    public void TestRoutingKeyExpression()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ProducerOptions producerProperties = GetProducerOptions("output", rabbitBindingsOptions);
        RabbitProducerOptions rabbitProducerOptions = rabbitBindingsOptions.GetRabbitProducerOptions("output");
        rabbitProducerOptions.RoutingKeyExpression = "Payload.field";

        DirectChannel output = CreateBindableChannel("output", CreateProducerBindingOptions(producerProperties));
        output.ComponentName = "rkeProducer";
        IBinding producerBinding = binder.BindProducer("rke", output, producerProperties);

        var admin = new RabbitAdmin(GetResource());
        Queue queue = new AnonymousQueue();
        var exchange = new TopicExchange("rke");
        Steeltoe.Messaging.RabbitMQ.Config.IBinding binding = BindingBuilder.Bind(queue).To(exchange).With("rkeTest");
        admin.DeclareQueue(queue);
        admin.DeclareBinding(binding);

        output.AddInterceptor(new TestChannelInterceptor
        {
            PreSendHandler = (message, _) =>
            {
                Assert.Equal("rkeTest", message.Headers[RabbitExpressionEvaluatingInterceptor.RoutingKeyHeader]);
                return message;
            }
        });

        output.Send(Message.Create(new Poco("rkeTest")));

        object bytes = SpyOn(queue.QueueName).Receive(false);

        Assert.IsType<byte[]>(bytes);

        Assert.Equal("{\"field\":\"rkeTest\"}", ((byte[])bytes).GetString());

        producerBinding.UnbindAsync();
    }

    [Fact]
    public void TestRoutingKeyExpressionPartitionedAndDelay()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ProducerOptions producerProperties = GetProducerOptions("output", rabbitBindingsOptions);
        RabbitProducerOptions rabbitProducerOptions = rabbitBindingsOptions.GetRabbitProducerOptions("output");
        rabbitProducerOptions.RoutingKeyExpression = "#root.get_Payload().field";

        // requires delayed message exchange plugin; tested locally
        // producerProperties.Extension.DelayedExchange = true;
        rabbitProducerOptions.DelayExpression = "1000";
        producerProperties.PartitionKeyExpression = "0";

        DirectChannel output = CreateBindableChannel("output", CreateProducerBindingOptions(producerProperties));
        output.ComponentName = "rkeProducer";
        IBinding producerBinding = binder.BindProducer("rkep", output, producerProperties);

        var admin = new RabbitAdmin(GetResource());
        Queue queue = new AnonymousQueue();
        var exchange = new TopicExchange("rkep");
        Steeltoe.Messaging.RabbitMQ.Config.IBinding binding = BindingBuilder.Bind(queue).To(exchange).With("rkepTest-0");
        admin.DeclareQueue(queue);
        admin.DeclareBinding(binding);

        output.AddInterceptor(new TestChannelInterceptor
        {
            PreSendHandler = (message, _) =>
            {
                Assert.Equal("rkepTest", message.Headers[RabbitExpressionEvaluatingInterceptor.RoutingKeyHeader]);

                Assert.Equal(1000, message.Headers[RabbitExpressionEvaluatingInterceptor.DelayHeader]);
                return message;
            }
        });

        output.Send(Message.Create(new Poco("rkepTest")));

        object bytes = SpyOn(queue.QueueName).Receive(false);

        Assert.IsType<byte[]>(bytes);

        Assert.Equal("{\"field\":\"rkepTest\"}", ((byte[])bytes).GetString());
        producerBinding.UnbindAsync();
    }

    [Fact]
    public void TestPolledConsumer()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ISmartMessageConverter messageConverter = new CompositeMessageConverterFactory().MessageConverterForAllRegistered;
        var inboundBindTarget = new DefaultPollableMessageSource(binder.ApplicationContext, messageConverter);

        ConsumerOptions consumerOptions = GetConsumerOptions("input", rabbitBindingsOptions);
        IBinding binding = binder.BindPollableConsumer("pollable", "group", inboundBindTarget, consumerOptions);
        var template = new RabbitTemplate(GetResource());
        template.ConvertAndSend("pollable.group", "testPollable");

        bool polled = inboundBindTarget.Poll(new TestMessageHandler
        {
            OnHandleMessage = m =>
            {
                Assert.Equal("testPollable", m.Payload);
            }
        });

        int n = 0;

        while (n++ < 100 && !polled)
        {
            polled = inboundBindTarget.Poll(new TestMessageHandler
            {
                OnHandleMessage = m =>
                {
                    Assert.Equal("testPollable", m.Payload);
                }
            });
        }

        Assert.True(polled);
        binding.UnbindAsync();
    }

    [Fact]
    public void TestPolledConsumer_AbstractBinder()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ISmartMessageConverter messageConverter = new CompositeMessageConverterFactory().MessageConverterForAllRegistered;
        var inboundBindTarget = new DefaultPollableMessageSource(binder.ApplicationContext, messageConverter);

        ConsumerOptions consumerOptions = GetConsumerOptions("input", rabbitBindingsOptions);
        IBinding binding = binder.BindConsumer("pollable", "group", (object)inboundBindTarget, consumerOptions);
        Assert.True(binding is DefaultBinding<IPollableSource<IMessageHandler>>);
        binding.UnbindAsync();
    }

    [Fact]
    public void TestPolledConsumerRequeue()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ISmartMessageConverter messageConverter = new CompositeMessageConverterFactory().MessageConverterForAllRegistered;
        var inboundBindTarget = new DefaultPollableMessageSource(binder.ApplicationContext, messageConverter);

        ConsumerOptions properties = GetConsumerOptions("input", rabbitBindingsOptions);

        IBinding binding = binder.BindPollableConsumer("pollableRequeue", "group", inboundBindTarget, properties);
        var template = new RabbitTemplate(GetResource());
        template.ConvertAndSend("pollableRequeue.group", "testPollable");

        try
        {
            bool polled = false;
            int n = 0;

            while (n++ < 100 && !polled)
            {
                polled = inboundBindTarget.Poll(new TestMessageHandler
                {
                    OnHandleMessage = m =>
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

        bool isPolled = inboundBindTarget.Poll(new TestMessageHandler
        {
            OnHandleMessage = m =>
            {
                Assert.Equal("testPollable", m.Payload);
            }
        });

        Assert.True(isPolled);
        binding.UnbindAsync();
    }

    [Fact]
    public void TestPolledConsumerWithDlq()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ISmartMessageConverter messageConverter = new CompositeMessageConverterFactory().MessageConverterForAllRegistered;
        var inboundBindTarget = new DefaultPollableMessageSource(binder.ApplicationContext, messageConverter);
        ConsumerOptions properties = GetConsumerOptions("input", rabbitBindingsOptions);
        RabbitConsumerOptions rabbitConsumerProperties = rabbitBindingsOptions.GetRabbitConsumerOptions("input");
        properties.MaxAttempts = 2;
        properties.BackOffInitialInterval = 0;
        rabbitConsumerProperties.AutoBindDlq = true;
        IBinding binding = binder.BindPollableConsumer("pollableDlq", "group", inboundBindTarget, properties);
        var template = new RabbitTemplate(GetResource());
        template.ConvertAndSend("pollableDlq.group", "testPollable");

        try
        {
            int n = 0;

            while (n++ < 100)
            {
                inboundBindTarget.Poll(new TestMessageHandler
                {
                    OnHandleMessage = _ => throw new Exception("test DLQ")
                });

                Thread.Sleep(100);
            }
        }
        catch (MessageHandlingException e)
        {
            Assert.Equal("test DLQ", e.InnerException.Message);
        }

        IMessage deadLetter = template.Receive("pollableDlq.group.dlq", 10_000);
        Assert.NotNull(deadLetter);
        binding.UnbindAsync();
    }

    [Fact]
    public void TestPolledConsumerWithDlqNoRetry()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ISmartMessageConverter messageConverter = new CompositeMessageConverterFactory().MessageConverterForAllRegistered;
        var inboundBindTarget = new DefaultPollableMessageSource(binder.ApplicationContext, messageConverter);
        ConsumerOptions properties = GetConsumerOptions("input", rabbitBindingsOptions);
        RabbitConsumerOptions rabbitConsumerOptions = rabbitBindingsOptions.GetRabbitConsumerOptions("input");
        properties.MaxAttempts = 1;
        rabbitConsumerOptions.AutoBindDlq = true;
        IBinding binding = binder.BindPollableConsumer("pollableDlqNoRetry", "group", inboundBindTarget, properties);
        var template = new RabbitTemplate(GetResource());

        template.ConvertAndSend("pollableDlqNoRetry.group", "testPollable");

        try
        {
            int n = 0;

            while (n++ < 100)
            {
                inboundBindTarget.Poll(new TestMessageHandler
                {
                    OnHandleMessage = _ => throw new Exception("test DLQ")
                });

                Thread.Sleep(100);
            }
        }
        catch (MessageHandlingException e)
        {
            Assert.Equal("test DLQ", e.Message);
        }

        IMessage deadLetter = template.Receive("pollableDlqNoRetry.group.dlq", 10_000);
        Assert.NotNull(deadLetter);
        binding.UnbindAsync();
    }

    [Fact]
    public void TestPolledConsumerWithDlqRePub()
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ISmartMessageConverter messageConverter = new CompositeMessageConverterFactory().MessageConverterForAllRegistered;
        var inboundBindTarget = new DefaultPollableMessageSource(binder.ApplicationContext, messageConverter);
        ConsumerOptions properties = GetConsumerOptions("input", rabbitBindingsOptions);
        RabbitConsumerOptions rabbitConsumerOptions = rabbitBindingsOptions.GetRabbitConsumerOptions("input");
        properties.MaxAttempts = 2;
        properties.BackOffInitialInterval = 0;
        rabbitConsumerOptions.AutoBindDlq = true;
        rabbitConsumerOptions.RepublishToDlq = true;
        IBinding binding = binder.BindPollableConsumer("pollableDlqRePub", "group", inboundBindTarget, properties);
        var template = new RabbitTemplate(GetResource());
        template.ConvertAndSend("pollableDlqRePub.group", "testPollable");
        bool polled = false;
        int n = 0;

        while (n++ < 100 && !polled)
        {
            Thread.Sleep(100);

            polled = inboundBindTarget.Poll(new TestMessageHandler
            {
                OnHandleMessage = _ => throw new Exception("test DLQ")
            });
        }

        Assert.True(polled);
        IMessage deadLetter = template.Receive("pollableDlqRePub.group.dlq", 10_000);
        Assert.NotNull(deadLetter);
        binding.UnbindAsync();
    }

    // TestCustomBatchingStrategy - Test not ported because CustomBatching Strategy is not yet supported in Steeltoe
    protected override string GetEndpointRouting(object endpoint)
    {
        var spelExp = GetPropertyValue<SpelExpression>(endpoint, "RoutingKeyExpression");
        return spelExp.ExpressionString;
    }

    protected override void CheckRkExpressionForPartitionedModuleSpel(object endpoint)
    {
        string routingExpression = GetEndpointRouting(endpoint);
        string delimiter = GetDestinationNameDelimiter();
        string dest = $"{GetExpectedRoutingBaseDestination($"'part{delimiter}0'", "test")} + '-' + Headers['{BinderHeaders.PartitionHeader}']";

        Assert.Contains(dest, routingExpression);
    }

    protected override string GetExpectedRoutingBaseDestination(string name, string group)
    {
        return name;
    }

    protected override bool UsesExplicitRouting()
    {
        return true;
    }

    private void TestAutoBindDlqPartionedConsumerFirstWithRepublishGuts(bool withRetry)
    {
        var rabbitBindingsOptions = new RabbitBindingsOptions();
        RabbitTestBinder binder = GetBinder(rabbitBindingsOptions);
        ConsumerOptions consumerProperties = GetConsumerOptions("input", rabbitBindingsOptions);
        RabbitConsumerOptions rabbitConsumerOptions = rabbitBindingsOptions.GetRabbitConsumerOptions("input");

        RegisterGlobalErrorChannel(binder);

        rabbitConsumerOptions.Prefix = "bindertest.";
        rabbitConsumerOptions.AutoBindDlq = true;
        rabbitConsumerOptions.RepublishToDlq = true;
        rabbitConsumerOptions.RepublishDeliveryMode = MessageDeliveryMode.NonPersistent;
        consumerProperties.MaxAttempts = withRetry ? 2 : 1;
        consumerProperties.Partitioned = true;
        consumerProperties.InstanceIndex = 0;
        DirectChannel input0 = CreateBindableChannel("input", CreateConsumerBindingOptions(consumerProperties));
        input0.ComponentName = "test.input0DLQ";
        IBinding input0Binding = binder.BindConsumer("partPubDLQ.0", "dlqPartGrp", input0, consumerProperties);

        IBinding defaultConsumerBinding1 =
            binder.BindConsumer("partPubDLQ.0", "default", new QueueChannel(LoggerFactory.CreateLogger<QueueChannel>()), consumerProperties);

        consumerProperties.InstanceIndex = 1;

        DirectChannel input1 = CreateBindableChannel("input1", CreateConsumerBindingOptions(consumerProperties));
        input1.ComponentName = "test.input1DLQ";
        IBinding input1Binding = binder.BindConsumer("partPubDLQ.0", "dlqPartGrp", input1, consumerProperties);
        IBinding defaultConsumerBinding2 = binder.BindConsumer("partPubDLQ.0", "default", new QueueChannel(), consumerProperties);

        ProducerOptions producerProperties = GetProducerOptions("output", rabbitBindingsOptions);
        RabbitProducerOptions rabbitProducerOptions = rabbitBindingsOptions.GetRabbitProducerOptions("output");
        rabbitProducerOptions.Prefix = "bindertest.";
        rabbitProducerOptions.AutoBindDlq = true;

        binder.ApplicationContext.Register("pkExtractor", new TestPartitionSupport("pkExtractor"));
        binder.ApplicationContext.Register("pkSelector", new TestPartitionSupport("pkSelector"));

        producerProperties.PartitionKeyExtractorName = "pkExtractor";
        producerProperties.PartitionSelectorName = "pkSelector";
        producerProperties.PartitionCount = 2;
        BindingOptions bindingProperties = CreateProducerBindingOptions(producerProperties);

        DirectChannel output = CreateBindableChannel("output", bindingProperties);
        output.ComponentName = "test.output";
        IBinding outputBinding = binder.BindProducer("partPubDLQ.0", output, producerProperties);

        var latch0 = new CountdownEvent(1);

        input0.Subscribe(new TestMessageHandler
        {
            OnHandleMessage = _ =>
            {
                if (latch0.CurrentCount <= 0)
                {
                    throw new Exception("dlq");
                }

                latch0.Signal();
            }
        });

        var latch1 = new CountdownEvent(1);

        input1.Subscribe(new TestMessageHandler
        {
            OnHandleMessage = _ =>
            {
                if (latch1.CurrentCount <= 0)
                {
                    throw new Exception("dlq");
                }

                latch1.Signal();
            }
        });

        IApplicationContext context = binder.ApplicationContext;
        var boundErrorChannel = context.GetService<ISubscribableChannel>("bindertest.partPubDLQ.0.dlqPartGrp-0.errors");
        var globalErrorChannel = context.GetService<ISubscribableChannel>("errorChannel");

        var boundErrorChannelMessage = new AtomicReference<IMessage>();
        var globalErrorChannelMessage = new AtomicReference<IMessage>();
        var hasRecovererInCallStack = new AtomicBoolean(!withRetry);

        boundErrorChannel.Subscribe(new TestMessageHandler
        {
            OnHandleMessage = message =>
            {
                boundErrorChannelMessage.GetAndSet(message);
                string stackTrace = new StackTrace().ToString();
                hasRecovererInCallStack.GetAndSet(stackTrace.Contains("ErrorMessageSendingRecoverer"));
            }
        });

        globalErrorChannel.Subscribe(new TestMessageHandler
        {
            OnHandleMessage = message =>
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

        const string streamDlqName = "bindertest.partPubDLQ.0.dlqPartGrp.dlq";

        IMessage received = template.Receive(streamDlqName);
        Assert.NotNull(received);
        Assert.Equal("partPubDLQ.0-1", received.Headers["x-original-routingKey"]);
        Assert.DoesNotContain(BinderHeaders.PartitionHeader, received.Headers);
        Assert.Equal(MessageDeliveryMode.NonPersistent, received.Headers.ReceivedDeliveryMode().Value);

        output.Send(Message.Create(0));
        received = template.Receive(streamDlqName);
        Assert.NotNull(received);
        Assert.Equal("partPubDLQ.0-0", received.Headers["x-original-routingKey"]);
        Assert.DoesNotContain(BinderHeaders.PartitionHeader, received.Headers);

        // verify we got a message on the dedicated error channel and the global (via bridge)
        Thread.Sleep(2000);
        Assert.NotNull(boundErrorChannelMessage.Value);

        Assert.Equal(withRetry, hasRecovererInCallStack.Value);
        Assert.NotNull(globalErrorChannelMessage.Value);

        input0Binding.UnbindAsync();
        input1Binding.UnbindAsync();
        defaultConsumerBinding1.UnbindAsync();
        defaultConsumerBinding2.UnbindAsync();
        outputBinding.UnbindAsync();
    }

    private void RegisterGlobalErrorChannel(RabbitTestBinder binder)
    {
        IApplicationContext applicationContext = binder.ApplicationContext;

        var errorChannel = new BinderErrorChannel(applicationContext, IntegrationContextUtils.ErrorChannelBeanName,
            LoggerFactory.CreateLogger<BinderErrorChannel>());

        applicationContext.Register(IntegrationContextUtils.ErrorChannelBeanName, errorChannel);
    }
}
