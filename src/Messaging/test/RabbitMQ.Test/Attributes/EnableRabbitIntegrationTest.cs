// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Common.Retry;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Listener.Adapters;
using Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Support;
using Steeltoe.Messaging.RabbitMQ.Support.Converter;
using Steeltoe.Messaging.RabbitMQ.Test;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static Steeltoe.Messaging.RabbitMQ.Attributes.EnableRabbitIntegrationTest;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Attributes;

[Trait("Category", "Integration")]
public class EnableRabbitIntegrationTest : IClassFixture<StartupFixture>
{
    private readonly IApplicationContext _context;
    private readonly IServiceProvider _provider;
    private readonly StartupFixture _fixture;

    public EnableRabbitIntegrationTest(StartupFixture fix)
    {
        _fixture = fix;
        _provider = _fixture.Provider;
        _context = _provider.GetRequiredService<IApplicationContext>();
    }

    [Fact]
    public void AutoDeclare()
    {
        var template = _context.GetRabbitTemplate();
        var reply = template.ConvertSendAndReceive<string>("auto.exch", "auto.rk", "foo");
        Assert.StartsWith("FOO", reply);
        var myService = _context.GetService<MyService>();
        Assert.NotNull(myService);
        Assert.True(myService.ChannelBoundOk);
    }

    [Fact]
    public void AutoSimpleDeclare()
    {
        var template = _context.GetRabbitTemplate();
        var reply = template.ConvertSendAndReceive<string>("test.simple.declare", "foo");
        Assert.StartsWith("FOO", reply);
    }

    [Fact]
    public void AutoSimpleDeclareAnonymousQueue()
    {
        var registry = _context.GetService<IRabbitListenerEndpointRegistry>() as RabbitListenerEndpointRegistry;
        var container = registry.GetListenerContainer("anonymousQueue575") as DirectMessageListenerContainer;
        Assert.Single(container.GetQueueNames());

        var template = _context.GetRabbitTemplate();
        Assert.Equal("viaAnonymous:foo", template.ConvertSendAndReceive<string>(container.GetQueueNames()[0], "foo"));
        var messageListener = container.MessageListener as MessagingMessageListenerAdapter;
        Assert.NotNull(messageListener.RetryTemplate);
        Assert.NotNull(messageListener.RecoveryCallback);
    }

    // TODO: Assert on the expected test outcome and remove suppression. Beyond not crashing, this test ensures nothing about the system under test.
    [Fact]
#pragma warning disable S2699 // Tests should include assertions
    public void Tx()
#pragma warning restore S2699 // Tests should include assertions
    {
        // TODO:
        //    assertThat(AopUtils.isJdkDynamicProxy(this.txService)).isTrue();
        //    Baz baz = new Baz();
        //    baz.field = "baz";
        //    rabbitTemplate.setReplyTimeout(600000);
        //    assertThat(rabbitTemplate.convertSendAndReceive("auto.exch.tx", "auto.rk.tx", baz)).isEqualTo("BAZ: baz: auto.rk.tx");
    }

    [Fact]
    public async Task AutoStart()
    {
        var registry = _context.GetService<IRabbitListenerEndpointRegistry>() as RabbitListenerEndpointRegistry;
        var container = registry.GetListenerContainer("notStarted") as DirectMessageListenerContainer;
        Assert.NotNull(container);
        Assert.False(container.IsAutoStartup);
        Assert.False(container.IsRunning);
        container.IsAutoStartup = true;
        await registry.Start();
        Assert.True(container.IsRunning);
        await container.Stop();
    }

    [Fact]
    public void AutoDeclareFanout()
    {
        var template = _context.GetRabbitTemplate();
        var reply = template.ConvertSendAndReceive<string>("auto.exch.fanout", string.Empty, "foo");
        Assert.Equal("FOOFOO", reply);
    }

    [Fact]
    public void AutoDeclareAnonWitAtts()
    {
        var template = _context.GetRabbitTemplate();
        var received = template.ConvertSendAndReceive<string>("auto.exch", "auto.anon.atts.rk", "foo");
        Assert.StartsWith("foo:", received);
        var queue = new Queue(received.Substring(4), true, true, true);
        var admin = _context.GetRabbitAdmin();
        admin.DeclareQueue(queue);
    }

    [Fact]
    public void AutoDeclareAnon()
    {
        var template = _context.GetRabbitTemplate();
        var reply = template.ConvertSendAndReceive<string>("auto.exch", "auto.anon.rk", "foo");
        Assert.Equal("FOO", reply);
    }

    [Fact]
    public void SimpleEndpoint()
    {
        var template = _context.GetRabbitTemplate();
        var reply = template.ConvertSendAndReceive<string>("test.simple", "foo");
        Assert.Equal("FOO", reply);

        var containers = _context.GetService<IMessageListenerContainerCollection>("testGroup");
        Assert.Equal(2, containers.Containers.Count);
    }

    [Fact]
    public async Task SimpleDirectEndpoint()
    {
        var registry = _context.GetService<IRabbitListenerEndpointRegistry>() as RabbitListenerEndpointRegistry;
        var container = registry.GetListenerContainer("direct") as DirectMessageListenerContainer;
        Assert.False(container.IsRunning);
        await container.Start();
        var template = _context.GetRabbitTemplate();
        var reply = template.ConvertSendAndReceive<string>("test.simple.direct", "foo");
        Assert.StartsWith("FOOfoo", reply);
        Assert.Equal(2, container.ConsumersPerQueue);
    }

    [Fact]
    public void SimpleDirectEndpointWithConcurrency()
    {
        var template = _context.GetRabbitTemplate();
        var reply = template.ConvertSendAndReceive<string>("test.simple.direct2", "foo");
        Assert.StartsWith("FOOfoo", reply);
        var registry = _context.GetService<IRabbitListenerEndpointRegistry>() as RabbitListenerEndpointRegistry;
        var container = registry.GetListenerContainer("directWithConcurrency") as DirectMessageListenerContainer;
        Assert.Equal(3, container.ConsumersPerQueue);
    }

    [Fact]
    public void SimpleInheritanceMethod()
    {
        var template = _context.GetRabbitTemplate();
        var reply = template.ConvertSendAndReceive<string>("test.inheritance", "foo");
        Assert.Equal("FOO", reply);
    }

    [Fact]
    public void SimpleInheritanceClass()
    {
        var template = _context.GetRabbitTemplate();
        var reply = template.ConvertSendAndReceive<string>("test.inheritance.class", "foo");
        Assert.Equal("FOOBAR", reply);
    }

    [Fact]
    public void Commas()
    {
        var template = _context.GetRabbitTemplate();
        var reply = template.ConvertSendAndReceive<string>("test,with,commas", "foo");
        Assert.Equal("FOOfoo", reply);
        var commaContainers = _context.GetService<IMessageListenerContainerCollection>("commas");
        Assert.Single(commaContainers.Containers);
        var container = commaContainers.Containers[0] as DirectMessageListenerContainer;
        var queueNames = container.GetQueueNames();
        Assert.Contains("test.comma.1", queueNames);
        Assert.Contains("test.comma.2", queueNames);
        Assert.Contains("test.comma.3", queueNames);
        Assert.Contains("test.comma.4", queueNames);
        Assert.Contains("test,with,commas", queueNames);
        Assert.Equal(5, queueNames.Length);
    }

    [Fact]
    public void MultiListener()
    {
        var foo = new Foo
        {
            Field = "foo"
        };
        var template = _context.GetRabbitTemplate("jsonRabbitTemplate");
        var reply = template.ConvertSendAndReceive<string>("multi.exch", "multi.rk", foo);
        Assert.Equal("FOO: foo handled by default handler", reply);

        var bar = new Bar
        {
            Field = "bar"
        };
        template.ConvertAndSend("multi.exch", "multi.rk", bar);
        template.ReceiveTimeout = 10000;
        var reply2 = template.ReceiveAndConvert<string>("sendTo.replies");
        Assert.Equal("BAR: bar", reply2);

        bar.Field = "crash";
        template.ConvertAndSend("multi.exch", "multi.rk", bar);
        var reply3 = template.ReceiveAndConvert<string>("sendTo.replies");
        Assert.Equal("CRASHCRASH Test reply from error handler", reply3);
        bar.Field = "bar";

        var baz = new Baz
        {
            Field = "baz"
        };
        var reply4 = template.ConvertSendAndReceive<string>("multi.exch", "multi.rk", baz);
        Assert.Equal("BAZ: baz", reply4);

        var qux = new Qux
        {
            Field = "qux"
        };
        var beanMethodHeaders = new List<string>();
        var mpp = new MultiListenerMessagePostProcessor(beanMethodHeaders);
        template.SetAfterReceivePostProcessors(mpp);

        var reply5 = template.ConvertSendAndReceive<string>("multi.exch", "multi.rk", qux);
        Assert.Equal("QUX: qux: multi.rk", reply5);

        Assert.Equal(2, beanMethodHeaders.Count);
        Assert.Equal("MultiListenerService", beanMethodHeaders[0]);
        Assert.Equal("Qux", beanMethodHeaders[1]);
        template.RemoveAfterReceivePostProcessor(mpp);

        var reply6 = template.ConvertSendAndReceive<string>("multi.exch.tx", "multi.rk.tx", bar);
        Assert.Equal("BAR: barbar", reply6);
        var reply7 = template.ConvertSendAndReceive<string>("multi.exch.tx", "multi.rk.tx", baz);
        Assert.Equal("BAZ: bazbaz: multi.rk.tx", reply7);
        var multiBean = _context.GetService<MultiListenerService>();
        Assert.NotNull(multiBean);
        Assert.IsType<MultiListenerService>(multiBean.Bean);
        Assert.NotNull(multiBean.Method);
        Assert.Equal("Baz", multiBean.Method.Name);

        // assertThat(AopUtils.isJdkDynamicProxy(this.txClassLevel)).isTrue();
    }

    [Fact]
    public void MultiListenerJson()
    {
        var template = _context.GetRabbitTemplate("jsonRabbitTemplate");
        var bar = new Bar
        {
            Field = "bar"
        };
        var exchange = "multi.json.exch";
        var routingKey = "multi.json.rk";
        var reply = template.ConvertSendAndReceive<string>(exchange, routingKey, bar);
        Assert.Equal("BAR: barMultiListenerJsonService", reply);

        var baz = new Baz
        {
            Field = "baz"
        };
        reply = template.ConvertSendAndReceive<string>(exchange, routingKey, baz);
        Assert.Equal("BAZ: baz", reply);

        var qux = new Qux
        {
            Field = "qux"
        };
        reply = template.ConvertSendAndReceive<string>(exchange, routingKey, qux);
        Assert.Equal("QUX: qux: multi.json.rk", reply);

        template.ConvertAndSend(exchange, routingKey, bar);
        template.ReceiveTimeout = 10000;
        reply = template.ReceiveAndConvert<string>("sendTo.replies.spel");
        Assert.Equal("BAR: barMultiListenerJsonService", reply);
        var registry = _context.GetService<IRabbitListenerEndpointRegistry>() as RabbitListenerEndpointRegistry;
        var container = registry.GetListenerContainer("multi") as DirectMessageListenerContainer;
        Assert.NotNull(container);
        var listener = container.MessageListener as MessagingMessageListenerAdapter;
        Assert.NotNull(listener);
        Assert.NotNull(listener.ErrorHandler);
        Assert.True(listener.ReturnExceptions);
    }

    [Fact]
    public void EndpointWithHeader()
    {
        var template = _context.GetRabbitTemplate();
        var properties = new MessageHeaders(new Dictionary<string, object> { { "prefix", "prefix-" } });
        var request = MessageTestUtils.CreateTextMessage("foo", properties);
        var reply = template.SendAndReceive("test.header", request);
        Assert.Equal("prefix-FOO", MessageTestUtils.ExtractText(reply));
        Assert.True(reply.Headers.Get<bool>("replyMPPApplied"));
        Assert.Equal("MyService", reply.Headers.Get<string>("bean"));
        Assert.Equal("CapitalizeWithHeader", reply.Headers.Get<string>("method"));
    }

    [Fact]
    public void EndpointWithMessage()
    {
        var template = _context.GetRabbitTemplate();
        var properties = new MessageHeaders(new Dictionary<string, object> { { "prefix", "prefix-" } });
        var request = MessageTestUtils.CreateTextMessage("foo", properties);
        var reply = template.SendAndReceive("test.message", request);
        Assert.Equal("prefix-FOO", MessageTestUtils.ExtractText(reply));
    }

    [Fact]
    public void EndpointWithComplexReply()
    {
        var template = _context.GetRabbitTemplate();
        var context = _context.GetService<IApplicationContext>();
        var strategy = context.GetService<IConsumerTagStrategy>("consumerTagStrategy") as ConsumerTagStrategy;
        var properties = new MessageHeaders(new Dictionary<string, object> { { "foo", "fooValue" } });
        var request = MessageTestUtils.CreateTextMessage("content", properties);
        var reply = template.SendAndReceive("test.reply", request);
        Assert.Equal("content", MessageTestUtils.ExtractText(reply));
        Assert.Equal("fooValue", reply.Headers.Get<string>("foo"));
        Assert.StartsWith(strategy.TagPrefix, reply.Headers.Get<string>("bar"));
    }

    [Fact]
    public void SimpleEndpointWithSendTo()
    {
        var template = _context.GetRabbitTemplate();
        template.ConvertAndSend("test.sendTo", "bar");
        template.ReceiveTimeout = 10000;
        var result = template.ReceiveAndConvert<string>("test.sendTo.reply");
        Assert.NotNull(result);
        Assert.Equal("BAR", result);
    }

    [Fact]
    public void SimpleEndpointWithSendToSpel()
    {
        var template = _context.GetRabbitTemplate();
        template.ConvertAndSend("test.sendTo.spel", "bar");
        template.ReceiveTimeout = 10000;
        var result = template.ReceiveAndConvert<string>("test.sendTo.reply.spel");
        Assert.NotNull(result);
        Assert.Equal("BARbar", result);
    }

    [Fact(Skip = "SpEL")]
    public void SimpleEndpointWithSendToSpelRuntime()
    {
        var template = _context.GetRabbitTemplate();
        template.ConvertAndSend("test.sendTo.runtimespel", "spel");
        template.ReceiveTimeout = 10000;
        var result = template.ReceiveAndConvert<string>("test.sendTo.reply.runtimespel");
        Assert.NotNull(result);
        Assert.Equal("runtimespel", result);
    }

    [Fact(Skip = "SpEL")]
    public void SimpleEndpointWithSendToSpelRuntimeMessagingMessage()
    {
        var template = _context.GetRabbitTemplate();
        template.ConvertAndSend("test.sendTo.runtimespelsource", "spel");
        template.ReceiveTimeout = 10000;
        var result = template.ReceiveAndConvert<string>("test.sendTo.runtimespelsource.reply");
        Assert.NotNull(result);
        Assert.Equal("sourceEval", result);
    }

    [Fact]
    public void TestInvalidPojoConversion()
    {
        var template = _context.GetRabbitTemplate();
        _fixture.ErrorHandlerLatch.Reset();
        _fixture.ErrorHandlerError.Value = null;
        template.ConvertAndSend("test.invalidPojo", "bar");
        Assert.True(_fixture.ErrorHandlerLatch.Wait(TimeSpan.FromSeconds(10)));
        var exception = _fixture.ErrorHandlerError.Value;
        Assert.NotNull(exception);
        Assert.IsType<RabbitRejectAndDontRequeueException>(exception);
        var cause = exception.InnerException;
        Assert.IsType<ListenerExecutionFailedException>(cause);
        var cause2 = cause.InnerException;
        Assert.IsType<MessageConversionException>(cause2);
        Assert.Contains("Cannot convert from [String] to [DateTime]", cause2.Message);
    }

    [Fact]
    public void TestDifferentTypes()
    {
        var foo = new Foo1
        {
            Bar = "bar"
        };
        var template = _context.GetRabbitTemplate("jsonRabbitTemplate");
        var service = _context.GetService<MyService>();
        service.Latch.Reset();
        service.Foos.Clear();
        template.ConvertAndSend("differentTypes", foo);
        Assert.True(service.Latch.Wait(TimeSpan.FromSeconds(10)));
        Assert.NotEmpty(service.Foos);
        Assert.IsType<Foo2>(service.Foos[0]);
        var foo2 = (Foo2)service.Foos[0];
        Assert.Equal("bar", foo2.Bar);
        var registry = _context.GetService<IRabbitListenerEndpointRegistry>() as RabbitListenerEndpointRegistry;
        var container = registry.GetListenerContainer("different") as DirectMessageListenerContainer;
        Assert.NotNull(container);
        Assert.Equal(1, container.ConsumersPerQueue);
    }

    [Fact]
    public void TestDifferentTypesWithConcurrency()
    {
        var foo = new Foo1
        {
            Bar = "bar"
        };
        var template = _context.GetRabbitTemplate("jsonRabbitTemplate");
        var service = _context.GetService<MyService>();
        service.Latch.Reset();
        service.Foos.Clear();
        template.ConvertAndSend("differentTypes2", foo);
        Assert.True(service.Latch.Wait(TimeSpan.FromSeconds(10)));
        Assert.NotEmpty(service.Foos);
        Assert.IsType<Foo2>(service.Foos[0]);
        var foo2 = (Foo2)service.Foos[0];
        Assert.Equal("bar", foo2.Bar);
        var registry = _context.GetService<IRabbitListenerEndpointRegistry>() as RabbitListenerEndpointRegistry;
        var container = registry.GetListenerContainer("differentWithConcurrency") as DirectMessageListenerContainer;
        Assert.NotNull(container);
        Assert.Equal(3, container.ConsumersPerQueue);
    }

    [Fact]
    public void TestFanout()
    {
        var template = _context.GetRabbitTemplate();
        template.ConvertAndSend("test.metaFanout", string.Empty, "foo");
        var service = _context.GetService<FanoutListener>();
        Assert.True(service.Latch.Wait(TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public void TestHeadersExchange()
    {
        var template = _context.GetRabbitTemplate();
        var reply = template.ConvertSendAndReceive<string>("auto.headers", string.Empty, "foo", new TestHeadersExchangeMpp1());
        Assert.Equal("FOO", reply);
        var reply1 = template.ConvertSendAndReceive<string>("auto.headers", string.Empty, "bar", new TestHeadersExchangeMpp2());
        Assert.Equal("BAR", reply1);
    }

    [Fact]
    public async Task DeadLetterOnDefaultExchange()
    {
        var template = _context.GetRabbitTemplate();
        template.ConvertAndSend("amqp656", "foo");
        var reply = template.ReceiveAndConvert<string>("amqp656dlq", 10000);
        Assert.Equal("foo", reply);
        var admin = _context.GetRabbitAdmin();

        var client = new HttpClient();
        var authToken = Encoding.ASCII.GetBytes("guest:guest");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));

        var result = await client.GetAsync("http://localhost:15672/api/queues/%2F/" + "amqp656");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var content = await result.Content.ReadAsStringAsync();
        Assert.Contains("test-empty", content);
        Assert.Contains("test-null", content);
    }

    [Fact]
    public void ReturnExceptionWithRethrowAdapter()
    {
        var template = _context.GetRabbitTemplate();
        template.ThrowReceivedExceptions = true;
        var e = Assert.Throws<InvalidOperationException>(() => template.ConvertSendAndReceive<string>("test.return.exceptions", "foo"));
        Assert.Contains("return this", e.Message);
        template.ThrowReceivedExceptions = false;
        Assert.IsType<InvalidOperationException>(template.ConvertSendAndReceive<object>("test.return.exceptions", "foo"));
    }

    [Fact]
    public void ListenerErrorHandler()
    {
        var template = _context.GetRabbitTemplate();
        var reply = template.ConvertSendAndReceive<string>("test.pojo.errors", "foo");
        Assert.Equal("BAR", reply);
    }

    [Fact]
    public void ListenerErrorHandlerException()
    {
        var template = _context.GetRabbitTemplate();
        template.ThrowReceivedExceptions = true;
        var e = Assert.Throws<InvalidOperationException>(() => template.ConvertSendAndReceive<string>("test.pojo.errors2", "foo"));
        Assert.Contains("from error handler", e.Message);
        Assert.Contains("return this", e.InnerException.Message);
        template.ThrowReceivedExceptions = false;
        Assert.NotNull(_fixture.ErrorHandlerChannel.Value);
    }

    [Fact]
    public void TestGenericReturnTypes()
    {
        var template = _context.GetRabbitTemplate("jsonRabbitTemplate");
        var returned = template.ConvertSendAndReceive<List<JsonObject>>("test.generic.list", new JsonObject("baz"));
        Assert.NotNull(returned[0]);

        var returned1 = template.ConvertSendAndReceive<Dictionary<string, JsonObject>>("test.generic.map", new JsonObject("baz"));
        Assert.NotNull(returned1["key"]);
        Assert.IsType<JsonObject>(returned1["key"]);
    }

    [Fact]
    public void TestManualContainer()
    {
        var template = _context.GetRabbitTemplate();
        template.ConvertAndSend("test.manual.container", "foo");
        Assert.True(_fixture.ManualContainerLatch.Wait(TimeSpan.FromSeconds(10)));
        Assert.NotNull(_fixture.Message.Value);
        var msg = Encoding.UTF8.GetString((byte[])_fixture.Message.Value.Payload);
        Assert.Equal("foo", msg);
    }

    [Fact]
    public void TestNoListenerYet()
    {
        var template = _context.GetRabbitTemplate();
        template.ConvertAndSend("test.no.listener.yet", "bar");
        Assert.True(_fixture.NoListenerLatch.Wait(TimeSpan.FromSeconds(10)));
        Assert.NotNull(_fixture.Message.Value);
        var msg = Encoding.UTF8.GetString((byte[])_fixture.Message.Value.Payload);
        Assert.Equal("bar", msg);
    }

    [Fact]
    public void MessagingMessageReturned()
    {
        var message = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("\"messaging\""))
            .SetHeader(MessageHeaders.CONTENT_TYPE, "application/json")
            .Build();
        var template = _context.GetRabbitTemplate();
        message = template.SendAndReceive("test.messaging.message", message);
        Assert.NotNull(message);
        var str = Encoding.UTF8.GetString((byte[])message.Payload);
        Assert.Equal("{\"field\":\"MESSAGING\"}", str);
        Assert.Equal("bar", message.Headers.Get<string>("foo"));
    }

    [Fact]
    public void ByteArrayMessageReturned()
    {
        var message = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("amqp"))
            .SetHeader(MessageHeaders.CONTENT_TYPE, "text/plain")
            .Build();
        var template = _context.GetRabbitTemplate();
        message = template.SendAndReceive("test.amqp.message", message);
        Assert.NotNull(message);
        var str = Encoding.UTF8.GetString((byte[])message.Payload);
        Assert.Equal("AMQP", str);
        Assert.Equal("bar", message.Headers.Get<string>("foo"));
    }

    [Fact]
    public void BytesToString()
    {
        var message = Message.Create(Encoding.UTF8.GetBytes("bytes"));
        var template = _context.GetRabbitTemplate();
        var returned = template.SendAndReceive("test.bytes.to.string", message);
        Assert.NotNull(returned);
        Assert.IsType<byte[]>(returned.Payload);
        var str = Encoding.UTF8.GetString((byte[])returned.Payload);
        Assert.Equal("BYTES", str);
    }

    [Fact]
    public void TestManualOverride()
    {
        var registry = _context.GetService<IRabbitListenerEndpointRegistry>() as RabbitListenerEndpointRegistry;
        var container = registry.GetListenerContainer("manual.acks.1") as DirectMessageListenerContainer;
        Assert.Equal(AcknowledgeMode.MANUAL, container.AcknowledgeMode);
        var container2 = registry.GetListenerContainer("manual.acks.2") as DirectMessageListenerContainer;
        Assert.Equal(AcknowledgeMode.MANUAL, container2.AcknowledgeMode);
    }

    // TODO: Assert on the expected test outcome and remove suppression. Beyond not crashing, this test ensures nothing about the system under test.
    [Fact]
#pragma warning disable S2699 // Tests should include assertions
    public void TestConsumerBatchEnabled()
#pragma warning restore S2699 // Tests should include assertions
    {
        // TODO: Direct container does not support this aggregation of messages into a batch
        // var template = provider.GetRabbitTemplate();
        // template.ConvertAndSend("erit.batch.1", "foo");
        // template.ConvertAndSend("erit.batch.1", "bar");
        // template.ConvertAndSend("erit.batch.2", "foo");
        // template.ConvertAndSend("erit.batch.2", "bar");
        // template.ConvertAndSend("erit.batch.3", "foo");
        // template.ConvertAndSend("erit.batch.3", "bar");

        // var myService = provider.GetService<MyService>();
        // Assert.True(myService.Batch1Latch.Wait(TimeSpan.FromSeconds(10)));
        // Assert.True(myService.Batch2Latch.Wait(TimeSpan.FromSeconds(10)));
        // Assert.True(myService.Batch3Latch.Wait(TimeSpan.FromSeconds(10)));
        // Assert.Equal(2, myService.AmqpMessagesReceived.Count);
        // Assert.Equal(2, myService.MessagingMessagesReceived.Count);
        // Assert.Equal(2, myService.Batch3Strings.Count);
    }

    public class TestHeadersExchangeMpp1 : IMessagePostProcessor
    {
        public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
        {
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.SetHeader("foo", "bar");
            accessor.SetHeader("baz", "qux");
            return message;
        }

        public IMessage PostProcessMessage(IMessage message)
        {
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.SetHeader("foo", "bar");
            accessor.SetHeader("baz", "qux");
            return message;
        }
    }

    public class TestHeadersExchangeMpp2 : IMessagePostProcessor
    {
        public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
        {
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.SetHeader("baz", "fiz");
            return message;
        }

        public IMessage PostProcessMessage(IMessage message)
        {
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.SetHeader("baz", "fiz");
            return message;
        }
    }

    public class StartupFixture : IDisposable
    {
        private readonly CachingConnectionFactory _adminCf;
        private readonly RabbitAdmin _admin;
        private readonly IServiceCollection _services;

        public ServiceProvider Provider { get; set; }

        public CountdownEvent ManualContainerLatch { get; set; } = new (1);

        public CountdownEvent NoListenerLatch { get; set; } = new (1);

        public CountdownEvent ErrorHandlerLatch { get; set; } = new (1);

        public AtomicReference<Exception> ErrorHandlerError { get; set; } = new ();

        public AtomicReference<IMessage> Message { get; set; } = new ();

        public AtomicReference<RC.IModel> ErrorHandlerChannel { get; set; } = new ();

        public StartupFixture()
        {
            _adminCf = new CachingConnectionFactory("localhost");
            _admin = new RabbitAdmin(_adminCf);
            foreach (var q in Queues)
            {
                var queue = new Queue(q);
                _admin.DeclareQueue(queue);
            }

            _services = CreateContainer();
            Provider = _services.BuildServiceProvider();
            Provider.GetRequiredService<IHostedService>().StartAsync(default).Wait();
        }

        public void Dispose()
        {
            foreach (var q in Queues)
            {
                _admin.DeleteQueue(q);
            }

            _admin.DeleteQueue("sendTo.replies");
            _admin.DeleteQueue("sendTo.replies.spel");
            _adminCf.Dispose();

            Provider.Dispose();
        }

        public ServiceCollection CreateContainer(IConfiguration config = null)
        {
            var services = new ServiceCollection();
            config ??= new ConfigurationBuilder().Build();

            services.AddLogging(b =>
            {
                b.SetMinimumLevel(LogLevel.Debug);
                b.AddDebug();
                b.AddConsole();
            });

            services.AddSingleton(config);
            services.AddRabbitHostingServices();
            services.AddRabbitDefaultMessageConverter();
            services.AddRabbitMessageHandlerMethodFactory();
            services.AddRabbitListenerEndpointRegistry();
            services.AddRabbitListenerEndpointRegistrar();
            services.AddRabbitListenerAttributeProcessor();
            services.AddRabbitConnectionFactory();

            // Manual container, register as ISmartLifecycle so auto started
            services.AddSingleton<ISmartLifecycle>(p =>
            {
                var context = p.GetRequiredService<IApplicationContext>();
                var loggerFactory = p.GetService<ILoggerFactory>();
                var defFactory = context.GetService<IRabbitListenerContainerFactory>(DirectRabbitListenerContainerFactory.DEFAULT_SERVICE_NAME) as DirectRabbitListenerContainerFactory;
                var listener = new TestManualContainerListener(ManualContainerLatch, Message);
                var endpoint = new SimpleRabbitListenerEndpoint(context, listener, loggerFactory);
                endpoint.SetQueueNames("test.manual.container");
                var container = defFactory.CreateListenerContainer(endpoint);
                container.ServiceName = "factoryCreatedContainerSimpleListener";
                container.Initialize();
                return container;
            });

            // Manual container, register as ISmartLifecycle so auto started
            services.AddSingleton<ISmartLifecycle>(p =>
            {
                var context = p.GetRequiredService<IApplicationContext>();
                var defFactory = context.GetService<IRabbitListenerContainerFactory>(DirectRabbitListenerContainerFactory.DEFAULT_SERVICE_NAME); // as DirectRabbitListenerContainerFactory;
                var container = defFactory.CreateListenerContainer() as DirectMessageListenerContainer;
                container.ServiceName = "factoryCreatedContainerNoListener";
                container.SetQueueNames("test.no.listener.yet");
                container.MessageListener = new TestManualContainerListener(NoListenerLatch, Message);
                container.Initialize();
                return container;
            });

            // Add named container factory rabbitAutoStartFalseListenerContainerFactory
            services.AddRabbitListenerContainerFactory((p, f) =>
            {
                f.ServiceName = "rabbitAutoStartFalseListenerContainerFactory";
                f.AutoStartup = false;
            });

            // Add default container factory
            services.AddRabbitListenerContainerFactory((p, f) =>
            {
                var context = p.GetApplicationContext();
                f.ErrorHandler = context.GetService<IErrorHandler>("errorHandler");
                f.ConsumerTagStrategy = context.GetService<IConsumerTagStrategy>("consumerTagStrategy");
                f.RetryTemplate = new PollyRetryTemplate(3, 1, 1, 1);
                f.ReplyRecoveryCallback = new DefaultReplyRecoveryCallback();
                f.SetBeforeSendReplyPostProcessors(new AddSomeHeadersPostProcessor());
            });

            // Add named container factory txListenerContainerFactory
            services.AddRabbitListenerContainerFactory((p, f) =>
            {
                var context = p.GetApplicationContext();
                f.ServiceName = "txListenerContainerFactory";
                f.ErrorHandler = context.GetService<IErrorHandler>("errorHandler");
                f.ConsumerTagStrategy = context.GetService<IConsumerTagStrategy>("consumerTagStrategy");
                f.RetryTemplate = new PollyRetryTemplate(3, 1, 1, 1);
                f.ReplyRecoveryCallback = new DefaultReplyRecoveryCallback();
                f.IsChannelTransacted = true;
            });

            // Add named container factory directListenerContainerFactory
            services.AddRabbitListenerContainerFactory((p, f) =>
            {
                var context = p.GetApplicationContext();
                f.ServiceName = "directListenerContainerFactory";
                f.ConsumersPerQueue = 2;
                f.ErrorHandler = context.GetService<IErrorHandler>("errorHandler");
                f.ConsumerTagStrategy = context.GetService<IConsumerTagStrategy>("consumerTagStrategy");
            });

            // Add named container factory jsonListenerContainerFactory
            services.AddRabbitListenerContainerFactory((p, f) =>
            {
                var context = p.GetApplicationContext();
                f.ServiceName = "jsonListenerContainerFactory";
                f.ErrorHandler = context.GetService<IErrorHandler>("errorHandler");
                f.ConsumerTagStrategy = context.GetService<IConsumerTagStrategy>("consumerTagStrategy");
                f.MessageConverter = new JsonMessageConverter();
                f.RetryTemplate = new PollyRetryTemplate(3, 1, 1, 1);
                f.ReplyRecoveryCallback = new DefaultReplyRecoveryCallback();
                f.SetBeforeSendReplyPostProcessors(new AddSomeHeadersPostProcessor());
            });

            // Add named container factory consumerBatchContainerFactory
            services.AddRabbitListenerContainerFactory((p, f) =>
            {
                var context = p.GetApplicationContext();
                f.ServiceName = "consumerBatchContainerFactory";
                f.ConsumerTagStrategy = context.GetService<IConsumerTagStrategy>("consumerTagStrategy");
                f.BatchListener = true;

                // f.BatchingStrategy = new SimpleBatchingStrategy(2, 2048, 2000);
            });

            services.AddRabbitAdmin();
            services.AddRabbitTemplate();

            // Add named RabbitTemplate txRabbitTemplate
            services.AddRabbitTemplate((p, t) =>
            {
                t.ServiceName = "txRabbitTemplate";
                t.IsChannelTransacted = true;
            });

            // Add named RabbitTemplate jsonRabbitTemplate
            services.AddRabbitTemplate((p, t) =>
            {
                t.ServiceName = "jsonRabbitTemplate";
                t.MessageConverter = new JsonMessageConverter();
            });

            services.AddSingleton<MyService>();
            services.AddSingleton<IMyServiceInterface, MyServiceInterfaceImpl>();
            services.AddSingleton<IMyServiceInterface2, MyServiceInterfaceImpl2>();
            services.AddSingleton<ITxClassLevel, TxClassLevel>();
            services.AddSingleton<MultiListenerService>();
            services.AddSingleton<MultiListenerJsonService>();
            services.AddSingleton<FanoutListener>();

            services.AddRabbitListeners<MyService>(config);
            services.AddRabbitListeners<IMyServiceInterface>(config);
            services.AddRabbitListeners<IMyServiceInterface2>(config);
            services.AddRabbitListeners<MultiListenerService>(config);
            services.AddRabbitListeners<MultiListenerJsonService>(config);
            services.AddRabbitListeners<ITxClassLevel>(config);
            services.AddRabbitListeners<FanoutListener>(config);

            services.AddRabbitQueue(new Queue("sendTo.replies", false, false, false));
            services.AddRabbitQueue(new Queue("sendTo.replies.spel", false, false, false));

            services.AddRabbitQueues(
                new Queue("auto.headers1", true, false, true, new Dictionary<string, object> { { "x-message-ttl", 10000 } }),
                new Queue("auto.headers2", true, false, true, new Dictionary<string, object> { { "x-message-ttl", 10000 } }));
            services.AddRabbitExchange(new HeadersExchange("auto.headers", true, true));
            services.AddRabbitBindings(
                new QueueBinding("auto.headers1.binding", "auto.headers1", "auto.headers", string.Empty, new Dictionary<string, object> { { "x-match", "all" }, { "foo", "bar" }, { "baz", null } }),
                new QueueBinding("auto.headers2.binding", "auto.headers2", "auto.headers", string.Empty, new Dictionary<string, object> { { "x-match", "any" }, { "foo", "bax" }, { "baz", "fiz" } }));

            services.AddRabbitQueues(
                new Queue("amqp656", true, false, true, new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", string.Empty },
                    { "x-dead-letter-routing-key", "amqp656dlq" },
                    { "test-empty", string.Empty },
                    { "test-null", string.Empty }
                }));
            services.AddRabbitExchange(new TopicExchange("amqp656.topic", true, true));
            services.AddRabbitBindings(new QueueBinding("amqp656.binding", "amqp656", "amqp656.topic", "foo", null));

            services.AddSingleton<IErrorHandler>(p =>
            {
                var result = new ConditionalRejectingErrorHandler1(ErrorHandlerLatch, ErrorHandlerError)
                {
                    ServiceName = "errorHandler"
                };
                return result;
            });

            services.AddSingleton<IConsumerTagStrategy>(p =>
            {
                var result = new ConsumerTagStrategy
                {
                    ServiceName = "consumerTagStrategy"
                };
                return result;
            });

            services.AddRabbitListenerErrorHandler<UpcaseAndRepeatListenerErrorHandler>("upcaseAndRepeatErrorHandler");
            services.AddRabbitListenerErrorHandler<AlwaysBarListenerErrorHandler>("alwaysBARHandler");
            services.AddRabbitListenerErrorHandler("throwANewException", p => new ThrowANewExceptionErrorHandler(ErrorHandlerChannel));
            return services;
        }

        public static string[] Queues = new[]
        {
            "test.manual.container", "test.no.listener.yet",
            "test.simple", "test.header", "test.message", "test.reply", "test.sendTo", "test.sendTo.reply",
            "test.sendTo.spel", "test.sendTo.reply.spel", "test.sendTo.runtimespel", "test.sendTo.reply.runtimespel",
            "test.sendTo.runtimespelsource", "test.sendTo.runtimespelsource.reply",
            "test.intercepted", "test.intercepted.withReply",
            "test.invalidPojo", "differentTypes", "differentTypes2", "differentTypes3",
            "test.inheritance", "test.inheritance.class",
            "test.comma.1", "test.comma.2", "test.comma.3", "test.comma.4", "test,with,commas",
            "test.converted", "test.converted.list", "test.converted.array", "test.converted.args1",
            "test.converted.args2", "test.converted.message", "test.notconverted.message",
            "test.notconverted.channel", "test.notconverted.messagechannel", "test.notconverted.messagingmessage",
            "test.converted.foomessage", "test.notconverted.messagingmessagenotgeneric", "test.simple.direct",
            "test.simple.direct2", "test.generic.list", "test.generic.map",
            "amqp656dlq", "test.simple.declare", "test.return.exceptions", "test.pojo.errors", "test.pojo.errors2",
            "test.messaging.message", "test.amqp.message", "test.bytes.to.string", "test.projection",
            "manual.acks.1", "manual.acks.2", "erit.batch.1", "erit.batch.2", "erit.batch.3"
        };
    }

    [DeclareExchange(Name = "test.metaFanout", AutoDelete = "True", Type = ExchangeType.FANOUT)]
    public class FanoutListener
    {
        public CountdownEvent Latch { get; } = new (2);

        [DeclareAnonymousQueue("fanout1")]
        [DeclareQueueBinding(Name = "fanout1.binding", ExchangeName = "test.metaFanout", QueueName = "#{@fanout1}")]
        [RabbitListener(Binding = "fanout1.binding")]
        public void Handle1(string foo) => Latch.Signal();

        [DeclareAnonymousQueue("fanout2")]
        [DeclareQueueBinding(Name = "fanout2.binding", ExchangeName = "test.metaFanout", QueueName = "#{@fanout2}")]
        [RabbitListener(Binding = "fanout2.binding")]
        public void Handle2(string foo) => Latch.Signal();
    }

    public class MyService
    {
        private readonly IApplicationContext _context;

        public MyService(IApplicationContext context) => _context = context;

        public bool? ChannelBoundOk { get; set; }

        public CountdownEvent Latch { get; set; } = new (1);

        public List<object> Foos { get; set; } = new ();

        public CountdownEvent Batch1Latch { get; set; } = new (1);

        public CountdownEvent Batch2Latch { get; set; } = new (1);

        public CountdownEvent Batch3Latch { get; set; } = new (1);

        public List<IMessage<byte[]>> AmqpMessagesReceived { get; set; }

        public List<IMessage> MessagingMessagesReceived { get; set; }

        public List<string> Batch3Strings { get; set; }

        private RabbitTemplate TxRabbitTemplate => _context.GetRabbitTemplate("txRabbitTemplate");

        [DeclareQueue(Name = "auto.declare", AutoDelete = "True", Admin = "rabbitAdmin")]
        [DeclareExchange(Name = "auto.exch", AutoDelete = "True")]
        [DeclareQueueBinding(Name = "auto.binding", QueueName = "auto.declare", ExchangeName = "auto.exch", RoutingKey = "auto.rk")]
        [RabbitListener(Id = "threadNamer", Binding = "auto.binding", ContainerFactory = "txListenerContainerFactory")]
        public string HandleWithDeclare(string foo, RC.IModel channel)
        {
            ChannelBoundOk = TxRabbitTemplate.Execute(c => c.Equals(channel));
            return foo.ToUpper() + Thread.CurrentThread.Name;
        }

        [DeclareQueue(Name = "${jjjj?test.simple.declare}", Durable = "True")]
        [RabbitListener("${jjjj?test.simple.declare}")]
        public string HandleWithSimpleDeclare(string foo) => foo.ToUpper() + Thread.CurrentThread.Name;

        [DeclareAnonymousQueue("myAnonymous")]
        [RabbitListener(Queue = "#{@myAnonymous}", Id = "anonymousQueue575")]
        public string HandleWithAnonymousQueueToDeclare(string data) => $"viaAnonymous:{data}";

        [DeclareAnonymousQueue("anon1", AutoDelete = "True", Exclusive = "True", Durable = "True")]
        [DeclareExchange(Name = "auto.start", AutoDelete = "True", Delayed = "${no:prop?false}")]
        [DeclareQueueBinding(Name = "auto.start.binding", QueueName = "#{@anon1}", ExchangeName = "auto.start", RoutingKey = "auto.start")]
        [RabbitListener(Id = "notStarted", Binding = "auto.start.binding", ContainerFactory = "rabbitAutoStartFalseListenerContainerFactory")]
        public void HandleWithAutoStartFalse(string foo)
        {
        }

        [DeclareQueue(Name = "auto.declare.fanout", AutoDelete = "True")]
        [DeclareExchange(Name = "auto.exch.fanout", AutoDelete = "True", Type = ExchangeType.FANOUT)]
        [DeclareQueueBinding(Name = "auto.fanout.binding", QueueName = "auto.declare.fanout", ExchangeName = "auto.exch.fanout")]
        [RabbitListener(Binding = "auto.fanout.binding")]
        public string HandleWithFanout(string foo) => foo.ToUpper() + foo.ToUpper();

        [DeclareAnonymousQueue("anon2")]
        [DeclareExchange(Name = "auto.exch", AutoDelete = "True")]
        [DeclareQueueBinding(Name = "auto.exch.anon.rk", QueueName = "#{@anon2}", ExchangeName = "auto.exch", RoutingKey = "auto.anon.rk")]
        [RabbitListener(Binding = "auto.exch.anon.rk")]
        public string HandleWithDeclareAnon(string foo) => foo.ToUpper();

        [DeclareAnonymousQueue("anon3", AutoDelete = "True", Exclusive = "True", Durable = "True")]
        [DeclareExchange(Name = "auto.exch", AutoDelete = "True")]
        [DeclareQueueBinding(Name = "auto.exch.anon.atts.rk", QueueName = "#{@anon3}", ExchangeName = "auto.exch", RoutingKey = "auto.anon.atts.rk")]
        [RabbitListener(Binding = "auto.exch.anon.atts.rk")]
        public string HandleWithDeclareAnonQueueWithAtts(string foo, [Header(RabbitMessageHeaders.CONSUMER_QUEUE)] string queue) =>
            $"{foo}:{queue}";

        [RabbitListener("test.simple", Group = "testGroup")]
        public string Capitalize(string foo) => foo.ToUpper();

        [RabbitListener("test.header", Group = "testGroup")]
        public string CapitalizeWithHeader([Payload] string content, [Header] string prefix) => prefix + content.ToUpper();

        [RabbitListener("test.simple.direct", Id = "direct", AutoStartup = "${no:property:here?False}", ContainerFactory = "directListenerContainerFactory")]
        public string CapitalizeDirect1(string foo) => foo.ToUpper() + foo + Thread.CurrentThread.Name;

        [RabbitListener("test.simple.direct2", Id = "directWithConcurrency", Concurrency = "${ffffx?3}", ContainerFactory = "directListenerContainerFactory")]
        public string CapitalizeDirect2(string foo) => foo.ToUpper() + foo + Thread.CurrentThread.Name;

        [RabbitListener("test.comma.1", "test.comma.2", "test,with,commas", "test.comma.3", "test.comma.4", Group = "commas")]
        public string MultiQueuesConfig(string foo) => foo.ToUpper() + foo;

        [RabbitListener("test.message")]
        public string CapitalizeWithMessage(IMessage<string> message) => message.Headers.Get<string>("prefix") + message.Payload.ToUpper();

        [RabbitListener("test.reply")]
        public IMessage Reply(string payload, [Header] string foo, [Header(RabbitMessageHeaders.CONSUMER_TAG)] string tag)
            => RabbitMessageBuilder.WithPayload(payload).SetHeader("foo", foo).SetHeader("bar", tag).Build();

        [RabbitListener("test.sendTo")]
        [SendTo("${foo:bar?test.sendTo.reply}")]
        public string CapitalizeAndSendTo(string foo) => foo.ToUpper();

        [RabbitListener("test.sendTo.spel")]
        [SendTo("test.sendTo.reply.spel")]
        public string CapitalizeAndSendToSpel(string foo)
        {
            return foo.ToUpper() + foo;
        }

        [RabbitListener("test.sendTo.runtimespel")]
        [SendTo("!{'test.sendTo.reply.' + result}")]
        public string CapitalizeAndSendToSpelRuntime(string foo)
        {
            return $"runtime{foo}";
        }

        [RabbitListener("test.sendTo.runtimespelsource")]
        [SendTo("!{source.headers['amqp_consumerQueue'] + '.reply'}")] // TODO: Fix the hardcoded "amqp_consumerQueue" when this works
        public string CapitalizeAndSendToSpelRuntimeSource(string foo)
        {
            return "sourceEval";
        }

        [RabbitListener("test.invalidPojo")]
        public void HandleIt(DateTime body)
        {
        }

        [RabbitListener("differentTypes", Id = "different", ContainerFactory = "jsonListenerContainerFactory")]
        public void HandleDifferent(Foo2 foo)
        {
            Foos.Add(foo);
            Latch.Signal();
        }

        [RabbitListener("differentTypes2", Id = "differentWithConcurrency", ContainerFactory = "jsonListenerContainerFactory", Concurrency = "3")]
        public void HandleDifferentWithConcurrency(Foo2 foo)
        {
            Foos.Add(foo);
            Latch.Signal();
        }

        [RabbitListener(Bindings = new[] { "auto.headers1.binding", "auto.headers2.binding" })]
        public string HandleWithHeadersExchange(string foo) => foo.ToUpper();

        [RabbitListener(Id = "defaultDLX", Binding = "amqp656.binding")]
        public string HandleWithDeadLetterDefaultExchange(string foo) => throw new RabbitRejectAndDontRequeueException("dlq");

        [RabbitListener("test.return.exceptions", ReturnExceptions = "${some:prop?True}")]
        public string AlwaysFails(string data) => throw new InvalidOperationException("return this");

        [RabbitListener("test.pojo.errors", ErrorHandler = "#{@alwaysBARHandler}")]
        public string AlwaysFailsWithErrorHandler(string data) => throw new Exception("return this");

        [RabbitListener("test.pojo.errors2", ErrorHandler = "#{throwANewException}", ReturnExceptions = "True")]
        public string AlwaysFailsWithErrorHandlerThrowAnother(string data) => throw new Exception("return this");

        [RabbitListener("test.generic.list", ContainerFactory = "jsonListenerContainerFactory")]
        public List<JsonObject> GenericList(JsonObject input)
            => new () { input };

        [RabbitListener("test.generic.map", ContainerFactory = "jsonListenerContainerFactory")]
        public Dictionary<string, JsonObject> GenericMap(JsonObject input)
            => new () { { "key", input } };

        [RabbitListener("test.messaging.message", ContainerFactory = "jsonListenerContainerFactory")]
        public IMessage<Bar> MessagingMessage(string input)
        {
            var bar = new Bar
            {
                Field = input.ToUpper()
            };
            var headers = new MessageHeaders(new Dictionary<string, object> { { "foo", "bar" } });
            return Message.Create(bar, headers);
        }

        [RabbitListener("test.amqp.message")]
        public IMessage<byte[]> AmqpMessage(string input)
            => (IMessage<byte[]>)MessageBuilder.WithPayload(Encoding.UTF8.GetBytes(input.ToUpper())).SetHeader(MessageHeaders.CONTENT_TYPE, "text/plain").SetHeader("foo", "bar").Build();

        [RabbitListener("test.bytes.to.string")]
        public string BytesToString(string input) => input.ToUpper();

        [RabbitListener("manual.acks.1", Id = "manual.acks.1", AckMode = "MANUAL")]
        public string Manual1(string input, RC.IModel channel, [Header(RabbitMessageHeaders.DELIVERY_TAG)] ulong tag)
        {
            channel.BasicAck(tag, false);
            return input.ToUpper();
        }

        [RabbitListener("manual.acks.2", Id = "manual.acks.2", AckMode = "#{T(Steeltoe.Messaging.RabbitMQ.Core.AcknowledgeMode).MANUAL}")]
        public string Manual2(string input, RC.IModel channel, [Header(RabbitMessageHeaders.DELIVERY_TAG)] ulong tag)
        {
            channel.BasicAck(tag, false);
            return input.ToUpper();
        }

        [RabbitListener("erit.batch.1", ContainerFactory = "consumerBatchContainerFactory")]
        public void ConsumerBatch1(List<IMessage<byte[]>> amqpMessages)
        {
            AmqpMessagesReceived = amqpMessages;
            if (!Batch1Latch.IsSet)
            {
                Batch1Latch.Signal();
            }
        }

        [RabbitListener("erit.batch.2", ContainerFactory = "consumerBatchContainerFactory")]
        public void ConsumerBatch2(List<IMessage> messages)
        {
            MessagingMessagesReceived = messages;
            if (!Batch2Latch.IsSet)
            {
                Batch2Latch.Signal();
            }
        }

        [RabbitListener("erit.batch.3", ContainerFactory = "consumerBatchContainerFactory")]
        public void ConsumerBatch3(List<string> strings)
        {
            Batch3Strings = strings;
            if (!Batch3Latch.IsSet)
            {
                Batch3Latch.Signal();
            }
        }
    }

    public interface IMyServiceInterface
    {
        [RabbitListener("test.inheritance")]
        string TestAnnotationInheritance(string foo);
    }

    public class MyServiceInterfaceImpl : IMyServiceInterface
    {
        public string TestAnnotationInheritance(string foo) => foo.ToUpper();
    }

    [DeclareAnonymousQueue("TxClassLevel")]
    [DeclareExchange(Name = "multi.exch.tx", AutoDelete = "True")]
    [DeclareQueueBinding(Name = "multi.exch.binding.tx", ExchangeName = "multi.exch.tx", RoutingKey = "multi.rk.tx", QueueName = "#{@TxClassLevel}")]
    [RabbitListener(Binding = "multi.exch.binding.tx", ContainerFactory = "jsonListenerContainerFactory")]
    public interface ITxClassLevel
    {
        // @Transactional
        [RabbitHandler]
        string Foo(Bar bar);

        // @Transactional
        [RabbitHandler]
        string Baz([Payload] Baz baz, [Header(RabbitMessageHeaders.RECEIVED_ROUTING_KEY)] string rk);
    }

    public class TxClassLevel : ITxClassLevel
    {
        public string Foo(Bar bar) => $"BAR: {bar.Field}{bar.Field}";

        public string Baz(Baz baz, string rk) => $"BAZ: {baz.Field}{baz.Field}: {rk}";
    }

    public class JsonObject
    {
        public string Bar { get; set; }

        public JsonObject()
        {
        }

        public JsonObject(string bar) => Bar = bar;

        public override string ToString() => $"JsonObject [bar={Bar}]";
    }

    public class Foo
    {
        public string Field { get; set; }
    }

    public class Bar : Foo
    {
    }

    public class Baz : Foo
    {
    }

    public class Qux : Foo
    {
    }

    public class Foo1
    {
        public string Bar { get; set; }
    }

    public class Foo2
    {
        public string Bar { get; set; }

        public override string ToString() => $"bar={Bar}";
    }

    [DeclareAnonymousQueue("multiListenerAnon")]
    [DeclareExchange(Name = "multi.exch", AutoDelete = "True")]
    [DeclareQueueBinding(Name = "multi.exch.multi.listener", ExchangeName = "multi.exch", QueueName = "#{@multiListenerAnon}", RoutingKey = "multi.rk")]
    [RabbitListener(Binding = "multi.exch.multi.listener", ErrorHandler = "upcaseAndRepeatErrorHandler", ContainerFactory = "jsonListenerContainerFactory")]
    public class MultiListenerService
    {
        public object Bean { get; private set; }

        public MethodInfo Method { get; private set; }

        [RabbitHandler]
        [SendTo("${foo:bar?sendTo.replies}")]
        public string Bar(Bar bar)
        {
            if (bar.Field.Equals("crash"))
            {
                throw new Exception("Test reply from error handler");
            }

            return $"BAR: {bar.Field}";
        }

        [RabbitHandler]
        public string Baz(Baz baz, IMessage message)
        {
            Bean = message.Headers.Target();
            Method = message.Headers.TargetMethod();
            return $"BAZ: {baz.Field}";
        }

        [RabbitHandler]
        public string Qux([Header(RabbitMessageHeaders.RECEIVED_ROUTING_KEY)] string rk, [Payload] Qux qux)
            => $"QUX: {qux.Field}: {rk}";

        [RabbitHandler(true)]
        public string DefaultHandler([Payload] object payload) => payload is Foo foo
            ? $"FOO: {foo.Field} handled by default handler"
            : $"{payload} handled by default handler";
    }

    [RabbitListener("test.inheritance.class")]
    public interface IMyServiceInterface2
    {
        [RabbitHandler]
        string TestAnnotationInheritance(string foo);
    }

    public class MyServiceInterfaceImpl2 : IMyServiceInterface2
    {
        public string TestAnnotationInheritance(string foo) => $"{foo.ToUpper()}BAR";
    }

    [DeclareAnonymousQueue("multiListenerJson")]
    [DeclareExchange(Name = "multi.json.exch", AutoDelete = "True")]
    [DeclareQueueBinding(Name = "multi.exch.multi.json.listener", ExchangeName = "multi.json.exch", QueueName = "#{@multiListenerJson}", RoutingKey = "multi.json.rk")]
    [RabbitListener(Id = "multi", Binding = "multi.exch.multi.json.listener", ErrorHandler = "alwaysBARHandler", ContainerFactory = "jsonListenerContainerFactory", ReturnExceptions = "True")]
    public class MultiListenerJsonService
    {
        [RabbitHandler]
        [SendTo("sendTo.replies.spel")]
        public string Bar(Bar bar, IMessage message) =>
            $"BAR: {bar.Field}{message.Headers.Target().GetType().Name}";

        [RabbitHandler]
        public string Baz(Baz baz) => $"BAZ: {baz.Field}";

        [RabbitHandler]
        public string Qux([Header(RabbitMessageHeaders.RECEIVED_ROUTING_KEY)] string rk, [Payload] Qux qux) =>
            $"QUX: {qux.Field}: {rk}";
    }

    public class DefaultReplyRecoveryCallback : IRecoveryCallback
    {
        public object Recover(IRetryContext context) => null;
    }

    public class ConditionalRejectingErrorHandler1 : ConditionalRejectingErrorHandler
    {
        public ConditionalRejectingErrorHandler1(CountdownEvent errorHandlerLatch, AtomicReference<Exception> errorHandlerError)
        {
            ErrorHandlerLatch = errorHandlerLatch;
            ErrorHandlerError = errorHandlerError;
        }

        public CountdownEvent ErrorHandlerLatch { get; }

        public AtomicReference<Exception> ErrorHandlerError { get; }

        public override bool HandleError(Exception exception)
        {
            try
            {
                return base.HandleError(exception);
            }
            catch (Exception e)
            {
                ErrorHandlerError.Value = e;
                if (!ErrorHandlerLatch.IsSet)
                {
                    ErrorHandlerLatch.Signal();
                }

                throw;
            }
        }
    }

    public class AlwaysBarListenerErrorHandler : IRabbitListenerErrorHandler
    {
        public string ServiceName { get; set; } = nameof(AlwaysBarListenerErrorHandler);

        public object HandleError(IMessage amqpMessage, IMessage message, ListenerExecutionFailedException exception) => "BAR";
    }

    public class UpcaseAndRepeatListenerErrorHandler : IRabbitListenerErrorHandler
    {
        public string ServiceName { get; set; } = nameof(UpcaseAndRepeatListenerErrorHandler);

        public object HandleError(IMessage amqpMessage, IMessage message, ListenerExecutionFailedException exception)
        {
            var barPayload = message.Payload as Bar;
            var upperPayload = barPayload.Field.ToUpper();
            return $"{upperPayload}{upperPayload} {exception.InnerException.Message}";
        }
    }

    public class ThrowANewExceptionErrorHandler : IRabbitListenerErrorHandler
    {
        public ThrowANewExceptionErrorHandler(AtomicReference<RC.IModel> errorHandlerChannel) => ErrorHandlerChannel = errorHandlerChannel;

        public string ServiceName { get; set; } = "throwANewException";

        public AtomicReference<RC.IModel> ErrorHandlerChannel { get; }

        public object HandleError(IMessage amqpMessage, IMessage message, ListenerExecutionFailedException exception)
        {
            ErrorHandlerChannel.Value = message.Headers.Get<RC.IModel>(RabbitMessageHeaders.CHANNEL);
            throw new InvalidOperationException("from error handler", exception.InnerException);
        }
    }

    public class ConsumerTagStrategy : IConsumerTagStrategy
    {
        public string TagPrefix { get; } = Guid.NewGuid().ToString();

        private int _increment;

        public string ServiceName { get; set; }

        public string CreateConsumerTag(string queue) => TagPrefix + Interlocked.Increment(ref _increment);
    }

    public class MultiListenerMessagePostProcessor : IMessagePostProcessor
    {
        public MultiListenerMessagePostProcessor(List<string> serviceMethodHeaders) => ServiceMethodHeaders = serviceMethodHeaders;

        public List<string> ServiceMethodHeaders { get; }

        public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
        {
            ServiceMethodHeaders.Add(message.Headers.Get<string>("bean"));
            ServiceMethodHeaders.Add(message.Headers.Get<string>("method"));
            return message;
        }

        public IMessage PostProcessMessage(IMessage message)
        {
            ServiceMethodHeaders.Add(message.Headers.Get<string>("bean"));
            ServiceMethodHeaders.Add(message.Headers.Get<string>("method"));
            return message;
        }
    }

    public class AddSomeHeadersPostProcessor : IMessagePostProcessor
    {
        public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
        {
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.SetHeader("replyMPPApplied", true);
            accessor.SetHeader("bean", accessor.Target.GetType().Name);
            accessor.SetHeader("method", accessor.TargetMethod.Name);
            return message;
        }

        public IMessage PostProcessMessage(IMessage message)
        {
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.SetHeader("replyMPPApplied", true);
            accessor.SetHeader("bean", accessor.Target.GetType().Name);
            accessor.SetHeader("method", accessor.TargetMethod.Name);
            return message;
        }
    }

    public class TestManualContainerListener : IMessageListener
    {
        public CountdownEvent ManualContainerLatch { get; set; }

        public AtomicReference<IMessage> Message { get; set; }

        public TestManualContainerListener(CountdownEvent latch, AtomicReference<IMessage> message)
        {
            ManualContainerLatch = latch;
            Message = message;
        }

        public AcknowledgeMode ContainerAckMode { get; set; }

        public void OnMessage(IMessage message)
        {
            Message.Value = message;
            ManualContainerLatch.Signal();
        }

        public void OnMessageBatch(List<IMessage> messages)
        {
            Message.Value = messages[0];
            ManualContainerLatch.Signal();
        }
    }
}
