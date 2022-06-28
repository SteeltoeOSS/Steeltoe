// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Common.Util;
using Steeltoe.Integration;
using Steeltoe.Integration.Channel;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Binder.Rabbit.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Stream.Binder;

public abstract class PartitionCapableBinderTests<TTestBinder, TBinder> : AbstractBinderTests<TTestBinder, TBinder>
    where TTestBinder : AbstractTestBinder<TBinder>
    where TBinder : AbstractBinder<IMessageChannel>
{
    private readonly ILogger<PartitionCapableBinderTests<TTestBinder, TBinder>> _logger;

    protected PartitionCapableBinderTests(ITestOutputHelper output, ILoggerFactory loggerFactory)
        : base(output, loggerFactory)
    {
        _logger = loggerFactory?.CreateLogger<PartitionCapableBinderTests<TTestBinder, TBinder>>();
    }

    [Fact]
    public void TestAnonymousGroup()
    {
        var binder = GetBinder();
        var bindingsOptions = new RabbitBindingsOptions();
        var producerOptions = GetProducerOptions("input", bindingsOptions);
        var producerBindingOptions = CreateProducerBindingOptions(producerOptions);
        var output = CreateBindableChannel("output", producerBindingOptions);

        var consumerOptions = GetConsumerOptions("output", bindingsOptions);
        var delimiter = GetDestinationNameDelimiter();
        var producerBinding = binder.BindProducer($"defaultGroup{delimiter}0", output, producerBindingOptions.Producer);

        var input1 = new QueueChannel();
        var binding1 = binder.BindConsumer($"defaultGroup{delimiter}0", null, input1, consumerOptions);

        var input2 = new QueueChannel();
        var binding2 = binder.BindConsumer($"defaultGroup{delimiter}0", null, input2, consumerOptions);

        var testPayload1 = $"foo-{Guid.NewGuid()}";
        output.Send(MessageBuilder.WithPayload(testPayload1)
            .SetHeader(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN)
            .Build());

        var receivedMessage1 = (Message<byte[]>)Receive(input1);
        Assert.NotNull(receivedMessage1);
        Assert.Equal(testPayload1, Encoding.UTF8.GetString(receivedMessage1.Payload));

        var receivedMessage2 = (Message<byte[]>)Receive(input2);
        Assert.NotNull(receivedMessage2);
        Assert.Equal(testPayload1, Encoding.UTF8.GetString(receivedMessage2.Payload));

        binding2.Unbind();

        var testPayload2 = $"foo-{Guid.NewGuid()}";

        output.Send(MessageBuilder.WithPayload(testPayload2)
            .SetHeader(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN)
            .Build());

        binding2 = binder.BindConsumer($"defaultGroup{delimiter}0", null, input2, consumerOptions);
        var testPayload3 = $"foo-{Guid.NewGuid()}";
        output.Send(MessageBuilder.WithPayload(testPayload3)
            .SetHeader(MessageHeaders.CONTENT_TYPE, MimeTypeUtils.TEXT_PLAIN)
            .Build());

        receivedMessage1 = (Message<byte[]>)Receive(input1);
        Assert.NotNull(receivedMessage1);
        Assert.Equal(testPayload2, Encoding.UTF8.GetString(receivedMessage1.Payload));
        receivedMessage1 = (Message<byte[]>)Receive(input1);
        Assert.NotNull(receivedMessage1);
        Assert.NotNull(receivedMessage1.Payload);

        receivedMessage2 = (Message<byte[]>)Receive(input2);
        Assert.NotNull(receivedMessage2);
        Assert.Equal(testPayload3, Encoding.UTF8.GetString(receivedMessage2.Payload));

        producerBinding.Unbind();
        binding1.Unbind();
        binding2.Unbind();
    }

    [Fact]
    public void TestOneRequiredGroup()
    {
        var binder = GetBinder();
        var bindingsOptions = new RabbitBindingsOptions();
        var producerOptions = GetProducerOptions("input", bindingsOptions);
        var producerBindingOptions = CreateProducerBindingOptions(producerOptions);
        var output = CreateBindableChannel("output", producerBindingOptions);

        var consumerOptions = GetConsumerOptions("output", bindingsOptions);

        var testDestination = $"testDestination{Guid.NewGuid().ToString().Replace("-", string.Empty)}";
        producerOptions.RequiredGroups = new List<string> { "test1" };
        var producerBinding = binder.BindProducer(testDestination, output, producerOptions);
        var testPayload = $"foo-{Guid.NewGuid()}";

        output.Send(MessageBuilder.WithPayload(testPayload).SetHeader("contentType", MimeTypeUtils.TEXT_PLAIN).Build());
        var inbound1 = new QueueChannel();
        var consumerBinding = binder.BindConsumer(testDestination, "test1", inbound1, consumerOptions);
        var receivedMessage1 = (Message<byte[]>)Receive(inbound1);

        Assert.NotNull(receivedMessage1);
        Assert.Equal(testPayload, Encoding.UTF8.GetString(receivedMessage1.Payload));

        producerBinding.Unbind();
        consumerBinding.Unbind();
    }

    [Fact]
    public void TestTwoRequiredGroups()
    {
        var binder = GetBinder();
        var bindingsOptions = new RabbitBindingsOptions();
        var producerOptions = GetProducerOptions("input", bindingsOptions);
        var producerBindingOptions = CreateProducerBindingOptions(producerOptions);
        var output = CreateBindableChannel("output", producerBindingOptions);

        var testDestination = $"testDestination{Guid.NewGuid().ToString().Replace("-", string.Empty)}";
        producerOptions.RequiredGroups = new List<string> { "test1", "test2" };

        var producerBinding = binder.BindProducer(testDestination, output, producerOptions);

        var testPayload = $"foo-{Guid.NewGuid()}";

        output.Send(MessageBuilder.WithPayload(testPayload).SetHeader("contentType", MimeTypeUtils.TEXT_PLAIN).Build());
        var inbound1 = new QueueChannel();

        var consumerOptions = GetConsumerOptions("output", bindingsOptions);
        var consumerBinding1 = binder.BindConsumer(testDestination, "test1", inbound1, consumerOptions);

        var inbound2 = new QueueChannel();
        var consumerBinding2 = binder.BindConsumer(testDestination, "test2", inbound2, consumerOptions);

        var receivedMessage1 = (Message<byte[]>)Receive(inbound1);
        Assert.NotNull(receivedMessage1);
        Assert.Equal(testPayload, Encoding.UTF8.GetString(receivedMessage1.Payload));

        var receivedMessage2 = (Message<byte[]>)Receive(inbound2);

        Assert.NotNull(receivedMessage2);
        Assert.Equal(testPayload, Encoding.UTF8.GetString(receivedMessage2.Payload));

        consumerBinding1.Unbind();
        consumerBinding2.Unbind();
        producerBinding.Unbind();
    }

    [Fact]
    public void TestPartitionedModuleSpEL()
    {
        var bindingsOptions = new RabbitBindingsOptions();
        var binder = GetBinder(bindingsOptions);

        var consumerProperties = GetConsumerOptions("input", bindingsOptions);
        consumerProperties.Concurrency = 2;
        consumerProperties.InstanceIndex = 0;
        consumerProperties.InstanceCount = 3;
        consumerProperties.Partitioned = true;

        var delimiter = GetDestinationNameDelimiter();
        var input0 = new QueueChannel
        {
            ComponentName = "test.input0S"
        };

        var input0Binding = binder.BindConsumer($"part{delimiter}0", "testPartitionedModuleSpEL", input0, consumerProperties);

        consumerProperties.InstanceIndex = 1;
        var input1 = new QueueChannel { ComponentName = "test.input1S" };
        var input1Binding = binder.BindConsumer($"part{delimiter}0", "testPartitionedModuleSpEL", input1, consumerProperties);

        consumerProperties.InstanceIndex = 2;
        var input2 = new QueueChannel { ComponentName = "test.input2S" };
        var input2Binding = binder.BindConsumer($"part{delimiter}0", "testPartitionedModuleSpEL", input2, consumerProperties);

        var producerProperties = GetProducerOptions("output", bindingsOptions);
        var rabbitProducerOptions = bindingsOptions.GetRabbitProducerOptions("output");
        rabbitProducerOptions.RoutingKeyExpression = "'part.0'";
        producerProperties.PartitionKeyExpression = "Payload";
        producerProperties.PartitionSelectorExpression = "ToString()"; // For strings, Java hash is not equivalent to GetHashCode, but for 0,1,2 ToString() is equivalent to hash.
        producerProperties.PartitionCount = 3;
        var output = CreateBindableChannel("output", CreateProducerBindingOptions(producerProperties));
        output.ComponentName = "test.output";

        var outputBinding = binder.BindProducer($"part{delimiter}0", output, producerProperties);

        try
        {
            var endpoint = ExtractEndpoint(outputBinding);
            CheckRkExpressionForPartitionedModuleSpEL(endpoint);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, ex.Message);
        }

        var message2 = MessageBuilder.WithPayload("2").SetHeader("correlationId", "foo").SetHeader("contentType", MimeTypeUtils.TEXT_PLAIN).SetHeader("sequenceNumber", 42).SetHeader("sequenceSize", 43).Build();
        output.Send(message2);
        output.Send(MessageBuilder.WithPayload("1").SetHeader("contentType", MimeTypeUtils.TEXT_PLAIN).Build());
        output.Send(MessageBuilder.WithPayload("0").SetHeader("contentType", MimeTypeUtils.TEXT_PLAIN).Build());

        var receive0 = Receive(input0);
        Assert.NotNull(receive0);

        var receive1 = Receive(input1);
        Assert.NotNull(receive1);

        var receive2 = Receive(input2);
        Assert.NotNull(receive2);

        Func<IMessage, bool> correlationHeadersForPayload2 = m =>
        {
            var accessor = new IntegrationMessageHeaderAccessor(m);
            return "foo".Equals(accessor.GetCorrelationId()) && accessor.GetSequenceNumber() == 42 && accessor.GetSequenceSize() == 43;
        };
        if (UsesExplicitRouting())
        {
            Assert.Equal("0", ((byte[])receive0.Payload).GetString());
            Assert.Equal("1", ((byte[])receive1.Payload).GetString());
            Assert.Equal("2", ((byte[])receive2.Payload).GetString());
            Assert.True(correlationHeadersForPayload2(receive2));
        }
        else
        {
            var receivedMessages = new List<IMessage> { receive0, receive1, receive2 };
            Assert.Contains(receivedMessages, m => ((byte[])m.Payload).ToString() == "0");
            Assert.Contains(receivedMessages, m => ((byte[])m.Payload).ToString() == "1");
            Assert.Contains(receivedMessages, m => ((byte[])m.Payload).ToString() == "2");

            Func<IMessage, bool> payloadIs2 = m => m.Payload.Equals("2".GetBytes());
            Assert.Single(receivedMessages.Where(payloadIs2).Where(correlationHeadersForPayload2));
        }

        input0Binding.Unbind();
        input1Binding.Unbind();
        input2Binding.Unbind();
        outputBinding.Unbind();
    }

    protected abstract string GetEndpointRouting(object endpoint);

    protected abstract string GetExpectedRoutingBaseDestination(string name, string group);

    protected abstract bool UsesExplicitRouting();

    protected ILifecycle ExtractEndpoint(IBinding binding)
    {
        return GetFieldValue<ILifecycle>(binding, "_lifecycle");
    }

    protected TValue GetFieldValue<TValue>(object current, string name)
    {
        var fi = current.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        return (TValue)fi.GetValue(current);
    }

    protected TValue GetPropertyValue<TValue>(object current, string name)
    {
        var pi = current.GetType().GetProperty(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        return (TValue)pi.GetValue(current);
    }

    protected virtual void CheckRkExpressionForPartitionedModuleSpEL(object endpoint)
    {
        var routingExpression = GetEndpointRouting(endpoint);
        var delimiter = GetDestinationNameDelimiter();
        var dest = $"{GetExpectedRoutingBaseDestination($"part{delimiter}0", "test")}-' + Headers['partition']";
        Assert.Contains(dest, routingExpression);
    }
}
