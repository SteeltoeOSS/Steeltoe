// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Common.Util;
using Steeltoe.Integration;
using Steeltoe.Integration.Channel;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Binder.Rabbit.Config;
using Steeltoe.Stream.Config;
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
        TTestBinder binder = GetBinder();
        var bindingsOptions = new RabbitBindingsOptions();
        ProducerOptions producerOptions = GetProducerOptions("input", bindingsOptions);
        BindingOptions producerBindingOptions = CreateProducerBindingOptions(producerOptions);
        DirectChannel output = CreateBindableChannel("output", producerBindingOptions);

        ConsumerOptions consumerOptions = GetConsumerOptions("output", bindingsOptions);
        string delimiter = GetDestinationNameDelimiter();
        IBinding producerBinding = binder.BindProducer($"defaultGroup{delimiter}0", output, producerBindingOptions.Producer);

        var input1 = new QueueChannel();
        IBinding binding1 = binder.BindConsumer($"defaultGroup{delimiter}0", null, input1, consumerOptions);

        var input2 = new QueueChannel();
        IBinding binding2 = binder.BindConsumer($"defaultGroup{delimiter}0", null, input2, consumerOptions);

        string testPayload1 = $"foo-{Guid.NewGuid()}";
        output.Send(MessageBuilder.WithPayload(testPayload1).SetHeader(MessageHeaders.ContentType, MimeTypeUtils.TextPlain).Build());

        var receivedMessage1 = (Message<byte[]>)Receive(input1);
        Assert.NotNull(receivedMessage1);
        Assert.Equal(testPayload1, Encoding.UTF8.GetString(receivedMessage1.Payload));

        var receivedMessage2 = (Message<byte[]>)Receive(input2);
        Assert.NotNull(receivedMessage2);
        Assert.Equal(testPayload1, Encoding.UTF8.GetString(receivedMessage2.Payload));

        binding2.Unbind();

        string testPayload2 = $"foo-{Guid.NewGuid()}";

        output.Send(MessageBuilder.WithPayload(testPayload2).SetHeader(MessageHeaders.ContentType, MimeTypeUtils.TextPlain).Build());

        binding2 = binder.BindConsumer($"defaultGroup{delimiter}0", null, input2, consumerOptions);
        string testPayload3 = $"foo-{Guid.NewGuid()}";
        output.Send(MessageBuilder.WithPayload(testPayload3).SetHeader(MessageHeaders.ContentType, MimeTypeUtils.TextPlain).Build());

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
        TTestBinder binder = GetBinder();
        var bindingsOptions = new RabbitBindingsOptions();
        ProducerOptions producerOptions = GetProducerOptions("input", bindingsOptions);
        BindingOptions producerBindingOptions = CreateProducerBindingOptions(producerOptions);
        DirectChannel output = CreateBindableChannel("output", producerBindingOptions);

        ConsumerOptions consumerOptions = GetConsumerOptions("output", bindingsOptions);

        string testDestination = $"testDestination{Guid.NewGuid().ToString().Replace("-", string.Empty)}";

        producerOptions.RequiredGroups = new List<string>
        {
            "test1"
        };

        IBinding producerBinding = binder.BindProducer(testDestination, output, producerOptions);
        string testPayload = $"foo-{Guid.NewGuid()}";

        output.Send(MessageBuilder.WithPayload(testPayload).SetHeader("contentType", MimeTypeUtils.TextPlain).Build());
        var inbound1 = new QueueChannel();
        IBinding consumerBinding = binder.BindConsumer(testDestination, "test1", inbound1, consumerOptions);
        var receivedMessage1 = (Message<byte[]>)Receive(inbound1);

        Assert.NotNull(receivedMessage1);
        Assert.Equal(testPayload, Encoding.UTF8.GetString(receivedMessage1.Payload));

        producerBinding.Unbind();
        consumerBinding.Unbind();
    }

    [Fact]
    public void TestTwoRequiredGroups()
    {
        TTestBinder binder = GetBinder();
        var bindingsOptions = new RabbitBindingsOptions();
        ProducerOptions producerOptions = GetProducerOptions("input", bindingsOptions);
        BindingOptions producerBindingOptions = CreateProducerBindingOptions(producerOptions);
        DirectChannel output = CreateBindableChannel("output", producerBindingOptions);

        string testDestination = $"testDestination{Guid.NewGuid().ToString().Replace("-", string.Empty)}";

        producerOptions.RequiredGroups = new List<string>
        {
            "test1",
            "test2"
        };

        IBinding producerBinding = binder.BindProducer(testDestination, output, producerOptions);

        string testPayload = $"foo-{Guid.NewGuid()}";

        output.Send(MessageBuilder.WithPayload(testPayload).SetHeader("contentType", MimeTypeUtils.TextPlain).Build());
        var inbound1 = new QueueChannel();

        ConsumerOptions consumerOptions = GetConsumerOptions("output", bindingsOptions);
        IBinding consumerBinding1 = binder.BindConsumer(testDestination, "test1", inbound1, consumerOptions);

        var inbound2 = new QueueChannel();
        IBinding consumerBinding2 = binder.BindConsumer(testDestination, "test2", inbound2, consumerOptions);

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
    public void TestPartitionedModuleSpel()
    {
        var bindingsOptions = new RabbitBindingsOptions();
        TTestBinder binder = GetBinder(bindingsOptions);

        ConsumerOptions consumerProperties = GetConsumerOptions("input", bindingsOptions);
        consumerProperties.Concurrency = 2;
        consumerProperties.InstanceIndex = 0;
        consumerProperties.InstanceCount = 3;
        consumerProperties.Partitioned = true;

        string delimiter = GetDestinationNameDelimiter();

        var input0 = new QueueChannel
        {
            ComponentName = "test.input0S"
        };

        IBinding input0Binding = binder.BindConsumer($"part{delimiter}0", "testPartitionedModuleSpEL", input0, consumerProperties);

        consumerProperties.InstanceIndex = 1;

        var input1 = new QueueChannel
        {
            ComponentName = "test.input1S"
        };

        IBinding input1Binding = binder.BindConsumer($"part{delimiter}0", "testPartitionedModuleSpEL", input1, consumerProperties);

        consumerProperties.InstanceIndex = 2;

        var input2 = new QueueChannel
        {
            ComponentName = "test.input2S"
        };

        IBinding input2Binding = binder.BindConsumer($"part{delimiter}0", "testPartitionedModuleSpEL", input2, consumerProperties);

        ProducerOptions producerProperties = GetProducerOptions("output", bindingsOptions);
        RabbitProducerOptions rabbitProducerOptions = bindingsOptions.GetRabbitProducerOptions("output");
        rabbitProducerOptions.RoutingKeyExpression = "'part.0'";
        producerProperties.PartitionKeyExpression = "Payload";

        producerProperties.PartitionSelectorExpression =
            "ToString()"; // For strings, Java hash is not equivalent to GetHashCode, but for 0,1,2 ToString() is equivalent to hash.

        producerProperties.PartitionCount = 3;
        DirectChannel output = CreateBindableChannel("output", CreateProducerBindingOptions(producerProperties));
        output.ComponentName = "test.output";

        IBinding outputBinding = binder.BindProducer($"part{delimiter}0", output, producerProperties);

        try
        {
            ILifecycle endpoint = ExtractEndpoint(outputBinding);
            CheckRkExpressionForPartitionedModuleSpel(endpoint);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, ex.Message);
        }

        IMessage message2 = MessageBuilder.WithPayload("2").SetHeader("correlationId", "foo").SetHeader("contentType", MimeTypeUtils.TextPlain)
            .SetHeader("sequenceNumber", 42).SetHeader("sequenceSize", 43).Build();

        output.Send(message2);
        output.Send(MessageBuilder.WithPayload("1").SetHeader("contentType", MimeTypeUtils.TextPlain).Build());
        output.Send(MessageBuilder.WithPayload("0").SetHeader("contentType", MimeTypeUtils.TextPlain).Build());

        IMessage receive0 = Receive(input0);
        Assert.NotNull(receive0);

        IMessage receive1 = Receive(input1);
        Assert.NotNull(receive1);

        IMessage receive2 = Receive(input2);
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
            var receivedMessages = new List<IMessage>
            {
                receive0,
                receive1,
                receive2
            };

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
        return GetFieldValue<ILifecycle>(binding, "Lifecycle");
    }

    protected TValue GetFieldValue<TValue>(object current, string name)
    {
        FieldInfo fi = current.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        return (TValue)fi.GetValue(current);
    }

    protected TValue GetPropertyValue<TValue>(object current, string name)
    {
        PropertyInfo pi = current.GetType().GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        return (TValue)pi.GetValue(current);
    }

    protected virtual void CheckRkExpressionForPartitionedModuleSpel(object endpoint)
    {
        string routingExpression = GetEndpointRouting(endpoint);
        string delimiter = GetDestinationNameDelimiter();
        string dest = $"{GetExpectedRoutingBaseDestination($"part{delimiter}0", "test")}-' + Headers['partition']";
        Assert.Contains(dest, routingExpression);
    }
}
